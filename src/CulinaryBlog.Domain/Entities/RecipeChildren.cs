// Tệp này định nghĩa các thực thể con của công thức.
// Chức năng: mô hình hóa nguyên liệu (RecipeIngredient), bước làm (RecipeStep) và ảnh (RecipeImage).

namespace CulinaryBlog.Domain.Entities;

// Class biểu diễn một nguyên liệu và thứ tự hiển thị trong công thức.
// Input: tên, lượng, đơn vị, ghi chú và RecipeId. Output: thực thể RecipeIngredient.
public sealed class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
    public int OrderIndex { get; set; }
}

// Class biểu diễn một bước thực hiện có thứ tự, thời gian và ảnh tùy chọn.
// Input: nội dung bước và RecipeId. Output: thực thể RecipeStep.
public sealed class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int StepNumber { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int? TimerMinutes { get; set; }
    public string? ImageUrl { get; set; }
}

// Class biểu diễn ảnh công thức, gồm URL, alt text, thứ tự và cờ ảnh đại diện.
// Input: thông tin tệp và RecipeId. Output: thực thể RecipeImage.
public sealed class RecipeImage : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string OriginalUrl { get; set; } = "";
    public string? MediumUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int OrderIndex { get; set; }
}
