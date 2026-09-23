namespace CulinaryBlog.API.Controllers;

using System.Security.Claims;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/v1/recipes")]
public sealed class RecipesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RecipePageResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] RecipeDifficulty? difficulty = null,
        [FromQuery] bool mine = false,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new { errorCode = "INVALID_PAGINATION", message = "page phải >= 1 và pageSize từ 1 đến 100." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (mine && userId is null) return Unauthorized();

        var recipes = db.Recipes.AsNoTracking().AsQueryable();
        recipes = mine
            ? (isAdmin ? recipes : recipes.Where(x => x.AuthorId == userId))
            : recipes.Where(x => x.Status == RecipeStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            recipes = recipes.Where(x => EF.Functions.ILike(x.Title, term) || EF.Functions.ILike(x.Description, term));
        }
        if (categoryId.HasValue) recipes = recipes.Where(x => x.CategoryId == categoryId);
        if (difficulty.HasValue) recipes = recipes.Where(x => x.Difficulty == difficulty);

        var descending = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        recipes = sortBy.ToLowerInvariant() switch
        {
            "title" => descending ? recipes.OrderByDescending(x => x.Title) : recipes.OrderBy(x => x.Title),
            "publishedat" => descending ? recipes.OrderByDescending(x => x.PublishedAt) : recipes.OrderBy(x => x.PublishedAt),
            "preptime" => descending ? recipes.OrderByDescending(x => x.PrepTime) : recipes.OrderBy(x => x.PrepTime),
            "cooktime" => descending ? recipes.OrderByDescending(x => x.CookTime) : recipes.OrderBy(x => x.CookTime),
            "createdat" => descending ? recipes.OrderByDescending(x => x.CreatedAt) : recipes.OrderBy(x => x.CreatedAt),
            _ => throw new BadHttpRequestException("sortBy không hợp lệ.")
        };

        var totalCount = await recipes.CountAsync(cancellationToken);
        var items = await (from recipe in recipes
                           join author in db.Users.AsNoTracking() on recipe.AuthorId equals author.Id
                           select new RecipeListItem(
                               recipe.Id, recipe.Title, recipe.Slug, recipe.Description,
                               recipe.PrepTime, recipe.CookTime, recipe.Servings,
                               recipe.Difficulty, recipe.Status, recipe.PublishedAt,
                               recipe.CategoryId, recipe.Category.Name,
                               recipe.AuthorId, author.DisplayName,
                               recipe.Images.Where(i => i.IsPrimary)
                                   .Select(i => i.ThumbnailUrl ?? i.MediumUrl ?? i.OriginalUrl).FirstOrDefault(),
                               recipe.CreatedAt))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new RecipePageResponse(items, totalCount, page, pageSize, totalPages, page < totalPages, page > 1));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.AsNoTracking().AsSplitQuery()
            .Include(x => x.Category).Include(x => x.Ingredients)
            .Include(x => x.Steps).Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        if (recipe is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (recipe.Status != RecipeStatus.Published && recipe.AuthorId != userId && !User.IsInRole("Admin"))
            return NotFound();

        var authorName = await db.Users.AsNoTracking().Where(x => x.Id == recipe.AuthorId)
            .Select(x => x.DisplayName).SingleAsync(cancellationToken);
        return Ok(new RecipeDetailResponse(
            recipe.Id, recipe.Title, recipe.Slug, recipe.Description, recipe.Instructions,
            recipe.PrepTime, recipe.CookTime, recipe.Servings, recipe.Difficulty, recipe.Status,
            recipe.PublishedAt, recipe.CategoryId, recipe.Category.Name, recipe.AuthorId, authorName,
            recipe.Nutrition,
            recipe.Ingredients.OrderBy(x => x.OrderIndex).Select(x => new RecipeIngredientItem(x.Id, x.Name, x.Quantity, x.Unit, x.Notes, x.OrderIndex)),
            recipe.Steps.OrderBy(x => x.StepNumber).Select(x => new RecipeStepItem(x.Id, x.StepNumber, x.Title, x.Description, x.TimerMinutes, x.ImageUrl)),
            recipe.Images.OrderBy(x => x.OrderIndex).Select(x => new RecipeImageItem(x.Id, x.OriginalUrl, x.MediumUrl, x.ThumbnailUrl, x.AltText, x.IsPrimary, x.OrderIndex)),
            Convert.ToBase64String(recipe.RowVersion), recipe.CreatedAt, recipe.UpdatedAt));
    }
}

public sealed record RecipePageResponse(IReadOnlyList<RecipeListItem> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasNextPage, bool HasPreviousPage);
public sealed record RecipeListItem(Guid Id, string Title, string Slug, string Description, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, RecipeStatus Status, DateTimeOffset? PublishedAt, Guid CategoryId, string CategoryName, string AuthorId, string AuthorName, string? PrimaryImageUrl, DateTimeOffset CreatedAt);
public sealed record RecipeIngredientItem(Guid Id, string Name, decimal? Quantity, string? Unit, string? Notes, int OrderIndex);
public sealed record RecipeStepItem(Guid Id, int StepNumber, string Title, string Description, int? TimerMinutes, string? ImageUrl);
public sealed record RecipeImageItem(Guid Id, string OriginalUrl, string? MediumUrl, string? ThumbnailUrl, string? AltText, bool IsPrimary, int OrderIndex);
public sealed record RecipeDetailResponse(Guid Id, string Title, string Slug, string Description, string Instructions, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, RecipeStatus Status, DateTimeOffset? PublishedAt, Guid CategoryId, string CategoryName, string AuthorId, string AuthorName, RecipeNutrition Nutrition, IEnumerable<RecipeIngredientItem> Ingredients, IEnumerable<RecipeStepItem> Steps, IEnumerable<RecipeImageItem> Images, string RowVersion, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);