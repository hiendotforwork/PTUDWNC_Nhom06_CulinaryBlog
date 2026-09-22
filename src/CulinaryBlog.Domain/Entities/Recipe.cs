namespace CulinaryBlog.Domain.Entities;

public class Recipe : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }

    public Guid CategoryId { get; set; }
    public Guid AuthorId { get; set; }

    public Category Category { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeImage> Images { get; set; } = new List<RecipeImage>();
}
