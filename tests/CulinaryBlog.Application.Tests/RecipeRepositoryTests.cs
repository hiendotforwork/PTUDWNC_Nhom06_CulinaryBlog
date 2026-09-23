using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Recipes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeRepositoryTests
{
    [Fact]
    public async Task FiltersByCategory()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Breakfast", Slug = "breakfast" };
        await using var context = CreateContext(category);
        await SeedAsync(context, category);

        var result = await CreateRepository(context).GetPublishedAsync(
            new GetRecipesQuery(CategoryId: category.Id));

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(category.Id, item.CategoryId));
    }

    [Fact]
    public async Task CombinesFiltersWithAndLogic()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Breakfast", Slug = "breakfast" };
        await using var context = CreateContext(category);
        await SeedAsync(context, category);

        var result = await CreateRepository(context).GetPublishedAsync(
            new GetRecipesQuery(
                CategoryId: category.Id,
                Difficulty: "Easy",
                MaxCookTime: 30,
                MinServings: 2));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("quick-eggs", result.Items.Single().Slug);
    }

    [Fact]
    public async Task FiltersByCookTimeWithoutIncludingPrepTime()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Breakfast", Slug = "breakfast" };
        await using var context = CreateContext(category);
        context.Recipes.Add(new Recipe
        {
            Id = Guid.NewGuid(),
            Slug = "long-prep-quick-cook",
            Title = "Long Prep Quick Cook",
            Description = "Recipe for testing cook time filtering",
            PrepTime = 45,
            CookTime = 10,
            Servings = 2,
            Difficulty = RecipeDifficulty.Easy,
            Status = RecipeStatus.Published,
            CategoryId = category.Id
        });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetPublishedAsync(
            new GetRecipesQuery(MaxCookTime: 10));

        Assert.Single(result.Items);
        Assert.Equal("long-prep-quick-cook", result.Items.Single().Slug);
    }

    [Fact]
    public async Task SortsByDescendingTitle()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Breakfast", Slug = "breakfast" };
        await using var context = CreateContext(category);
        await SeedAsync(context, category);

        var result = await CreateRepository(context).GetPublishedAsync(
            new GetRecipesQuery(Sort: "-title", PageSize: 10));

        Assert.Equal(["quick-eggs", "slow-pho"], result.Items.Select(item => item.Slug));
    }

    [Fact]
    public async Task SortsBeforePaginationAndKeepsPublishedOnly()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Breakfast", Slug = "breakfast" };
        await using var context = CreateContext(category);
        await SeedAsync(context, category);

        var result = await CreateRepository(context).GetPublishedAsync(
            new GetRecipesQuery(Page: 2, PageSize: 1, Sort: "-createdAt"));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Equal("slow-pho", result.Items.Single().Slug);
    }

    private static CulinaryBlogDbContext CreateContext(Category category)
    {
        var options = new DbContextOptionsBuilder<CulinaryBlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new CulinaryBlogDbContext(options);
        context.Categories.Add(category);
        return context;
    }

    private static async Task SeedAsync(CulinaryBlogDbContext context, Category category)
    {
        context.Recipes.AddRange(
            new Recipe
            {
                Id = Guid.NewGuid(),
                Slug = "slow-pho",
                Title = "Phở Bò",
                Description = "Slow recipe",
                PrepTime = 30,
                CookTime = 120,
                Servings = 4,
                Difficulty = RecipeDifficulty.Hard,
                Status = RecipeStatus.Published,
                CategoryId = category.Id,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Recipe
            {
                Id = Guid.NewGuid(),
                Slug = "quick-eggs",
                Title = "Trứng Chiên",
                Description = "Quick recipe",
                PrepTime = 5,
                CookTime = 10,
                Servings = 2,
                Difficulty = RecipeDifficulty.Easy,
                Status = RecipeStatus.Published,
                CategoryId = category.Id,
                CreatedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new Recipe
            {
                Id = Guid.NewGuid(),
                Slug = "draft-recipe",
                Title = "Draft",
                Description = "Must stay hidden",
                PrepTime = 1,
                CookTime = 1,
                Servings = 1,
                Difficulty = RecipeDifficulty.Easy,
                Status = RecipeStatus.Draft,
                CategoryId = category.Id,
                CreatedAt = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)
            });

        await context.SaveChangesAsync();
    }

    private static RecipeRepository CreateRepository(CulinaryBlogDbContext context) => new(context);
}