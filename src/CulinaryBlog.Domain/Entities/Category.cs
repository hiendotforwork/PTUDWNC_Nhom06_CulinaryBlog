namespace CulinaryBlog.Domain.Entities;

public sealed class Category : BaseEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public List<Recipe> Recipes { get; set; } = [];
}
