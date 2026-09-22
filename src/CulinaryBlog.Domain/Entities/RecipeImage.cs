namespace CulinaryBlog.Domain.Entities;

public class RecipeImage : BaseEntity
{
    public Guid RecipeId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? AltText { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
