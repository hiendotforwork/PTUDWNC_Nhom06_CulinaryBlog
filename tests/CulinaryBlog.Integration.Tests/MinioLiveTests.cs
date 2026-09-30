using CulinaryBlog.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Minio;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class MinioLiveFactAttribute : FactAttribute
{
    public MinioLiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_MINIO_TESTS") != "1")
            Skip = "Set RUN_MINIO_TESTS=1 after starting compose.storage.yml.";
    }
}

public sealed class MinioLiveTests
{
    [MinioLiveFact]
    public async Task UploadPublicReadAndIdempotentDeleteAgainstLocalMinio()
    {
        var options = new MinioStorageOptions
        {
            Endpoint = "localhost:9000",
            PublicBaseUrl = "http://localhost:9000",
            AccessKey = Environment.GetEnvironmentVariable("MINIO_ROOT_USER") ?? throw new InvalidOperationException("Set MINIO_ROOT_USER."),
            SecretKey = Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD") ?? throw new InvalidOperationException("Set MINIO_ROOT_PASSWORD.")
        };
        using var client = new MinioClient().WithEndpoint(options.Endpoint).WithCredentials(options.AccessKey, options.SecretKey).Build();
        var storage = new MinioFileStorageService(client, Options.Create(options));
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aF3sAAAAASUVORK5CYII=");
        using var image = new MemoryStream(bytes);
        var stored = await storage.UploadAsync(image, "image/png", ".png", "recipes/" + Guid.NewGuid(), default);
        using var http = new HttpClient();
        try
        {
            using var result = await http.GetAsync(stored.Url);
            result.EnsureSuccessStatusCode();
            Assert.Equal("image/png", result.Content.Headers.ContentType?.MediaType);
            Assert.Equal(bytes, await result.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            await storage.DeleteAsync(stored.Url, default);
            await storage.DeleteAsync(stored.Url, default);
        }
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await http.GetAsync(stored.Url)).StatusCode);
    }
}
