namespace CulinaryBlog.Domain.Entities;

public class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; set; }
    public int StepNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
