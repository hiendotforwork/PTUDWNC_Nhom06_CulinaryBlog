// Tệp này định nghĩa hợp đồng lưu trữ tệp cho module ảnh công thức.
// Chức năng: tải tệp (UploadAsync) và xóa tệp (DeleteAsync).

namespace CulinaryBlog.Application.Interfaces;

public sealed record StoredFile(string Url);

// Interface tách nghiệp vụ ảnh khỏi cách lưu local, MinIO hoặc dịch vụ khác.
// Input: stream/thông tin tệp hoặc URL. Output: StoredFile hoặc Task hoàn tất.
public interface IFileStorageService
{
    // Chức năng: lưu một luồng tệp vào thư mục chỉ định.
    // Input: stream, contentType, extension, folder và cancellationToken. Output: StoredFile chứa URL.
    Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken);
    // Chức năng: xóa tệp theo URL nếu tồn tại.
    // Input: url và cancellationToken. Output: Task hoàn tất.
    Task DeleteAsync(string url, CancellationToken cancellationToken);
}
