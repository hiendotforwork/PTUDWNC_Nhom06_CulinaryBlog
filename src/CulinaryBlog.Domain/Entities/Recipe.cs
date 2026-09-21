namespace CulinaryBlog.Domain.Entities;

public enum RecipeStatus : short { Draft, Published, Archived }
public enum RecipeDifficulty : short { Easy = 1, Medium, Hard, Expert }

public sealed class Recipe : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instructions { get; set; } = "";
    public int PrepTime { get; set; }
    public int CookTime { get; set; }
    public int Servings { get; set; }
    public RecipeDifficulty Difficulty { get; set; } = RecipeDifficulty.Easy;
    public RecipeStatus Status { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string AuthorId { get; set; } = "";
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
