using System.Collections.Concurrent;
using CulinaryBlog.Application.Files;
using CulinaryBlog.Application.Interfaces;

namespace CulinaryBlog.Integration.Tests;

public sealed class TestFileStorage : IFileStorageService, IFileDeletionQueue
{
    public ConcurrentDictionary<string, byte[]> Files { get; } = new();
    public ConcurrentQueue<string> Deletions { get; } = new();
    public async Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken ct)
    {
        using var image = await ImageFile.ReadAsync(stream, contentType, extension, ct);
        var url = "http://storage.test/culinary-blog/" + folder + "/" + Guid.NewGuid().ToString("N") + extension;
        Files[url] = image.ToArray();
        return new StoredFile(url);
    }
    public Task DeleteAsync(string url, CancellationToken ct) { Files.TryRemove(url, out _); return Task.CompletedTask; }
    public void Enqueue(string url) => Deletions.Enqueue(url);
}
