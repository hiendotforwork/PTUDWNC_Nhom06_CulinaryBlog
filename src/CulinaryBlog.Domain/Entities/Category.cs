namespace CulinaryBlog.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
