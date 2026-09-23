using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public sealed class Recipe : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int PrepTime { get; set; }
    public int CookTime { get; set; }
    public int Servings { get; set; }
    public RecipeDifficulty Difficulty { get; set; }
    public RecipeStatus Status { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string AuthorId { get; set; } = string.Empty;
    public RecipeNutrition Nutrition { get; set; } = new();
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<RecipeStep> Steps { get; set; } = [];
    public List<RecipeImage> Images { get; set; } = [];
}

public sealed class RecipeNutrition
{
    public decimal? Calories { get; set; }
    public decimal? Protein { get; set; }
    public decimal? Carbohydrates { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sodium { get; set; }
}
