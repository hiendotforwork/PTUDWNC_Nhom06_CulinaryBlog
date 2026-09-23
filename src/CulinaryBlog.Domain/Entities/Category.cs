// Tệp này định nghĩa thực thể danh mục của công thức.
// Chức năng: lưu thông tin danh mục và quan hệ một-nhiều với Recipe.

namespace CulinaryBlog.Domain.Entities;

// Class biểu diễn một danh mục món ăn.
// Input: dữ liệu thuộc tính từ nghiệp vụ/EF Core. Output: thực thể Category được lưu trong database.
public sealed class Category : BaseEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public List<Recipe> Recipes { get; set; } = [];
}
