namespace CulinaryBlog.Domain.Entities;

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
