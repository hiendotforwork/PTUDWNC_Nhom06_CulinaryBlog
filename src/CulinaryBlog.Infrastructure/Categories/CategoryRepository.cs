using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Repositories;
using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Categories;

public sealed class CategoryRepository(ApplicationDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.Recipes.Count(recipe => recipe.Status == RecipeStatus.Published)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDetailDto?> GetBySlugAsync(
        string slug,
        int page,
        int pageSize,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(item => item.Slug == slug)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Slug,
                item.Description,
                RecipeCount = item.Recipes.Count(recipe => recipe.Status == RecipeStatus.Published)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
            return null;

        var recipes = db.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.CategoryId == category.Id
                && (recipe.Status == RecipeStatus.Published
                    || currentUserId != null
                    && recipe.Status == RecipeStatus.Draft
                    && recipe.AuthorId == currentUserId));

        var totalCount = await recipes.CountAsync(cancellationToken);
        var items = await recipes
            .OrderByDescending(recipe => recipe.CreatedAt)
            .ThenBy(recipe => recipe.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                category.Name,
                recipe.CreatedAt))
            .ToListAsync(cancellationToken);

        return new CategoryDetailDto(
            new CategoryDto(category.Id, category.Name, category.Slug, category.Description, category.RecipeCount),
            PagedResult<RecipeSummaryDto>.Create(items, totalCount, page, pageSize));
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return db.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLowerInvariant();
        return db.Categories.AnyAsync(
            category => category.Name.ToLower() == normalizedName
                && (!excludingId.HasValue || category.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return db.Categories
            .IgnoreQueryFilters()
            .AnyAsync(category => category.Slug == slug, cancellationToken);
    }

    public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return db.Recipes.CountAsync(recipe => recipe.CategoryId == categoryId, cancellationToken);
    }

    public Task<int> CountPublishedRecipesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return db.Recipes.CountAsync(
            recipe => recipe.CategoryId == categoryId && recipe.Status == RecipeStatus.Published,
            cancellationToken);
    }

    public void Add(Category category) => db.Categories.Add(category);

    public void Update(Category category) => db.Categories.Update(category);

    public void SoftDelete(Category category) => category.IsDeleted = true;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}
