namespace CulinaryBlog.Application.Interfaces;

public sealed record StoredFile(string Url);

public interface IFileStorageService
{
    Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken);
    Task DeleteAsync(string url, CancellationToken cancellationToken);
}