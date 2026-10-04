using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Files;
using CulinaryBlog.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Moq;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class MinioFileStorageTests
{
    private readonly Mock<IMinioClient> client = new();
    private MinioFileStorageService Storage => new(client.Object, Options.Create(new MinioStorageOptions()));
    private static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aF3sAAAAASUVORK5CYII=");

    [Fact]
    public async Task UploadGeneratesUniqueOwnedUrlsAndCallsMinio()
    {
        using var first = new MemoryStream(Png);
        using var second = new MemoryStream(Png);
        var a = await Storage.UploadAsync(first, "image/png", ".png", "recipes/123", default);
        var b = await Storage.UploadAsync(second, "image/png", ".png", "recipes/123", default);
        Assert.StartsWith("http://localhost:9000/culinary-blog/recipes/123/", a.Url);
        Assert.NotEqual(a.Url, b.Url);
        client.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".exe")]
    [InlineData("text/plain", ".png")]
    public async Task RejectsMimeAndExtensionMismatch(string mime, string extension)
    {
        using var image = new MemoryStream(Png);
        await Assert.ThrowsAsync<InvalidImageException>(() => Storage.UploadAsync(image, mime, extension, "avatars/user1", default));
        client.Verify(x => x.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectsOversizeStream()
    {
        using var image = new MemoryStream(new byte[ImageFile.MaxBytes + 1]);
        await Assert.ThrowsAsync<InvalidImageException>(() => Storage.UploadAsync(image, "image/png", ".png", "recipes/1", default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../recipes/1")]
    [InlineData("recipes/../1")]
    [InlineData("avatars/user%2fadmin")]
    public async Task RejectsUnsafeFolder(string folder)
    {
        using var image = new MemoryStream(Png);
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.UploadAsync(image, "image/png", ".png", folder, default));
    }

    [Theory]
    [InlineData("http://evil.test/culinary-blog/recipes/1/image.png")]
    [InlineData("http://localhost:9000/other/recipes/1/image.png")]
    [InlineData("http://localhost:9000/culinary-blog/recipes/../image.png")]
    [InlineData("http://localhost:9000/culinary-blog/recipes/1/%2e%2e.png")]
    [InlineData("http://localhost:9000/culinary-blog/recipes/1/image.png?x=y")]
    public async Task RejectsUnownedDeleteUrl(string url)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Storage.DeleteAsync(url, default));
        client.Verify(x => x.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCanBeRepeated()
    {
        var url = "http://localhost:9000/culinary-blog/recipes/1/image.png";
        await Storage.DeleteAsync(url, default);
        await Storage.DeleteAsync(url, default);
        client.Verify(x => x.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StorageFailureIsMapped()
    {
        client.Setup(x => x.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        await Assert.ThrowsAsync<FileStorageException>(() => Storage.DeleteAsync("http://localhost:9000/culinary-blog/avatars/1/image.png", default));
    }

    [Fact]
    public async Task CallerCancellationIsPreserved()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var image = new MemoryStream(Png);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Storage.UploadAsync(image, "image/png", ".png", "avatars/1", cts.Token));
    }

    [Theory]
    [InlineData("image/jpeg", ".jpeg", "FFD8FF")]
    [InlineData("image/webp", ".webp", "524946460400000057454250")]
    [InlineData("image/avif", ".avif", "00000018667479706176696600000000617669666D696631")]
    public async Task AcceptsSupportedSignatures(string mime, string extension, string hex)
    {
        using var source = new MemoryStream(Convert.FromHexString(hex));
        using var validated = await ImageFile.ReadAsync(source, mime, extension, default);
        Assert.Equal(0, validated.Position);
    }
}
