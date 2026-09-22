using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Application.Recipes.Validation;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using System.Text;

namespace CulinaryBlog.Infrastructure.Recipes;

public sealed class RecipeRepository(CulinaryBlogDbContext dbContext) : IRecipeRepository
{
    public async Task<PagedResult<RecipeSummaryDto>> GetPublishedAsync(
        GetRecipesQuery query,
        CancellationToken cancellationToken = default)
    {
        var recipes = dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published);

        if (query.CategoryId.HasValue)
        {
            recipes = recipes.Where(recipe => recipe.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty)
            && Enum.TryParse<RecipeDifficulty>(query.Difficulty, true, out var difficulty))
        {
            recipes = recipes.Where(recipe => recipe.Difficulty == difficulty);
        }

        if (query.MinPrepTime.HasValue)
        {
            recipes = recipes.Where(recipe => recipe.PrepTime >= query.MinPrepTime.Value);
        }

        if (query.MaxCookTime.HasValue)
        {
            recipes = recipes.Where(recipe => recipe.PrepTime + recipe.CookTime <= query.MaxCookTime.Value);
        }

        if (query.MinServings.HasValue)
        {
            recipes = recipes.Where(recipe => recipe.Servings >= query.MinServings.Value);
        }

        var totalCount = await recipes.CountAsync(cancellationToken);
        recipes = ApplySorting(recipes, query.Sort);
        var items = await recipes
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(recipe => new RecipeSummaryDto(
                recipe.Id,
                recipe.Slug,
                recipe.Title,
                recipe.Description,
                recipe.PrepTime,
                recipe.CookTime,
                recipe.Servings,
                recipe.Difficulty.ToString(),
                recipe.CategoryId,
                recipe.Category.Name,
                recipe.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<RecipeSummaryDto>.Create(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<PagedResult<RecipeSummaryDto>> SearchPublishedAsync(
        SearchRecipesQuery query,
        CancellationToken cancellationToken = default)
    {
        var tsQuery = BuildPrefixQuery(query.Query);
        var recipes = dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published);

        recipes = recipes.Where(recipe =>
            EF.Property<NpgsqlTsVector>(recipe, "SearchVector").Matches(tsQuery));

        var totalCount = await recipes.CountAsync(cancellationToken);
        var items = await recipes
            .OrderByDescending(recipe =>
                EF.Functions.Rank(EF.Property<NpgsqlTsVector>(recipe, "SearchVector"), tsQuery))
            .ThenByDescending(recipe => recipe.CreatedAt)
            .ThenBy(recipe => recipe.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(recipe => new RecipeSummaryDto(
                recipe.Id,
                recipe.Slug,
                recipe.Title,
                recipe.Description,
                recipe.PrepTime,
                recipe.CookTime,
                recipe.Servings,
                recipe.Difficulty.ToString(),
                recipe.CategoryId,
                recipe.Category.Name,
                recipe.CreatedAt,
                EF.Functions.Rank(
                    EF.Property<NpgsqlTsVector>(recipe, "SearchVector"),
                    tsQuery)))
            .ToListAsync(cancellationToken);

        return PagedResult<RecipeSummaryDto>.Create(items, totalCount, query.Page, query.PageSize);
    }

    private static NpgsqlTsQuery BuildPrefixQuery(string query)
    {
        var terms = query
            .Normalize(NormalizationForm.FormC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(term => RemoveDiacritics(new string(term.Where(char.IsLetterOrDigit).ToArray())))
            .Where(term => term.Length > 0)
            .Select(term => $"{term}:*");

        return NpgsqlTsQuery.Parse(string.Join(" & ", terms));
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized.Where(character =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character)
                != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

        return withoutMarks.Replace('đ', 'd').Replace('Đ', 'D');
    }

    private static IQueryable<Recipe> ApplySorting(
        IQueryable<Recipe> recipes,
        string sort)
    {
        return sort switch
        {
            "createdAt" => recipes.OrderBy(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "-createdAt" => recipes.OrderByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "title" => recipes.OrderBy(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "-title" => recipes.OrderByDescending(recipe => recipe.Title).ThenBy(recipe => recipe.Id),
            "cookTime" => recipes.OrderBy(recipe => recipe.CookTime).ThenBy(recipe => recipe.Id),
            "-cookTime" => recipes.OrderByDescending(recipe => recipe.CookTime).ThenBy(recipe => recipe.Id),
            _ when GetRecipesQueryValidator.AllowedSorts.Contains(sort, StringComparer.OrdinalIgnoreCase)
                => ApplySorting(recipes, GetRecipesQueryValidator.AllowedSorts.First(allowed =>
                    allowed.Equals(sort, StringComparison.OrdinalIgnoreCase))),
            _ => throw new ArgumentException("Unsupported recipe sort.", nameof(sort))
        };
    }
}
