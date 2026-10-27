namespace CulinaryBlog.Integration.Tests;

using CulinaryBlog.API.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

public class LocalFileStorageTests
{
    private readonly LocalFileStorageService _storage;

    public LocalFileStorageTests()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());
        _storage = new LocalFileStorageService(envMock.Object);
    }

    [Fact]
    public void IsOwnedUrl_AcceptsGeneratedLocalAvatarUrl()
    {
        var validAvatarUrl = $"/uploads/avatars/user-123/{Guid.NewGuid():N}.jpg";
        var validRecipeUrl = $"/uploads/recipes/recipe-456/{Guid.NewGuid():N}.webp";

        Assert.True(_storage.IsOwnedUrl(validAvatarUrl));
        Assert.True(_storage.IsOwnedUrl(validRecipeUrl));
    }

    [Theory]
    [InlineData("https://lh3.googleusercontent.com/a/default-avatar")]
    [InlineData("https://storage.googleapis.com/avatars/user.jpg")]
    [InlineData("/uploads/avatars/../bad.jpg")]
    [InlineData("/uploads/other/user-123/0123456789abcdef0123456789abcdef.jpg")]
    [InlineData("/uploads/avatars/user-123/short.jpg")]
    [InlineData("/uploads/avatars/user-123/0123456789abcdef0123456789abcdef.exe")]
    [InlineData("")]
    [InlineData(null)]
    public void IsOwnedUrl_RejectsExternalAndMalformedPaths(string? url)
    {
        Assert.False(_storage.IsOwnedUrl(url!));
    }
}
