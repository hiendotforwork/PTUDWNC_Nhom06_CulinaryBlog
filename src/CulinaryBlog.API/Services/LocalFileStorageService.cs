namespace CulinaryBlog.API.Services;

using CulinaryBlog.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;

public sealed class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    public async Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken)
    {
        var root = Path.Combine(environment.ContentRootPath, "wwwroot");
        var relativeDirectory = Path.Combine("uploads", folder.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetFullPath(Path.Combine(root, relativeDirectory));
        var rootFullPath = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid storage path.");
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var output = File.Create(Path.Combine(directory, fileName));
        await stream.CopyToAsync(output, cancellationToken);
        return new StoredFile('/' + Path.Combine(relativeDirectory, fileName).Replace('\\', '/'));
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var root = Path.Combine(environment.ContentRootPath, "wwwroot");
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var rootFullPath = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (path.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}