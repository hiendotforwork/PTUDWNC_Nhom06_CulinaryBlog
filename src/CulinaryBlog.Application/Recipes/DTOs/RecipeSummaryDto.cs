namespace CulinaryBlog.Application.Recipes.DTOs;

public sealed record RecipeSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    int PrepTime,
    int CookTime,
    int Servings,
    string Difficulty,
    Guid CategoryId,
    string CategoryName,
    DateTimeOffset CreatedAt,
    double? RelevanceScore = null);
