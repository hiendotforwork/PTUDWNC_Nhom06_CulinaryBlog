// Tệp này định nghĩa các trường nền tảng dùng chung cho thực thể Recipe.
// Chức năng: cung cấp id, thời gian tạo/cập nhật, xóa mềm và RowVersion.

using System.Security.Cryptography;

namespace CulinaryBlog.Domain.Entities;

// Class cơ sở cho các thực thể có audit, xóa mềm và kiểm soát đồng thời.
// Input: không có. Output: các thuộc tính nền tảng được class con kế thừa.
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = RandomNumberGenerator.GetBytes(16);
}
