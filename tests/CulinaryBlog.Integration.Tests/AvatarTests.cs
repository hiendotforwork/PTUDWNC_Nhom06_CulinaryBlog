namespace CulinaryBlog.Integration.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using CulinaryBlog.API.Controllers;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Files;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Integration.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public sealed class AvatarTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static MultipartFormDataContent CreateImageForm(string mime = "image/png")
    {
        var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aF3sAAAAASUVORK5CYII="));
        image.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(image, "file", "avatar.png");
        return form;
    }

    private async Task<AuthResponse> RegisterUser(HttpClient client)
    {
        var email = $"avatar-{Guid.NewGuid():N}@example.com";
        var auth = await AuthTestHelpers.RegisterConfirmedUserAsync(
            client,
            factory.Services,
            new RegisterRequest(email, "avatar_" + Guid.NewGuid().ToString("N")[..8], "Avatar Test User", "Password123!"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    [Fact]
    public async Task UploadReplacementAndDeletion_EnqueueOwnedUrl_And_SkipExternalUrl()
    {
        using var client = factory.CreateClient();
        var auth = await RegisterUser(client);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = (TestFileStorage)scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        // Set an external avatar URL
        const string externalUrl = "https://lh3.googleusercontent.com/a/google-avatar";
        var user = await db.Users.SingleAsync(u => u.Id == auth.User.Id);
        user.AvatarUrl = externalUrl;
        await db.SaveChangesAsync();

        // Upload first avatar -> replaces external URL
        using var form1 = CreateImageForm();
        var postRes1 = await client.PostAsync("/api/v1/users/me/avatar", form1);
        postRes1.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstOwnedUrl = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == auth.User.Id)).AvatarUrl!;
        firstOwnedUrl.Should().StartWith("http://storage.test/culinary-blog/avatars/");
        storage.Deletions.Should().NotContain(externalUrl);

        // Upload replacement avatar -> replaces first owned avatar
        using var form2 = CreateImageForm();
        var postRes2 = await client.PostAsync("/api/v1/users/me/avatar", form2);
        postRes2.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondOwnedUrl = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == auth.User.Id)).AvatarUrl!;
        secondOwnedUrl.Should().NotBe(firstOwnedUrl);
        storage.Deletions.Should().Contain(firstOwnedUrl);

        // DELETE avatar -> deletes second owned avatar
        var delRes1 = await client.DeleteAsync("/api/v1/users/me/avatar");
        delRes1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        storage.Deletions.Should().Contain(secondOwnedUrl);

        var userAfterDelete = await db.Users.AsNoTracking().SingleAsync(u => u.Id == auth.User.Id);
        userAfterDelete.AvatarUrl.Should().BeNull();

        // Set another external URL and DELETE -> skips enqueuing external URL
        const string externalUrl2 = "https://storage.googleapis.com/ext/photo.jpg";
        db.ChangeTracker.Clear();
        user = await db.Users.SingleAsync(u => u.Id == auth.User.Id);
        user.AvatarUrl = externalUrl2;
        await db.SaveChangesAsync();

        var delRes2 = await client.DeleteAsync("/api/v1/users/me/avatar");
        delRes2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        storage.Deletions.Should().NotContain(externalUrl2);

        var finalUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == auth.User.Id);
        finalUser.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task FailedReplacement_LeavesOldUrlOnUser_AndCleansUpNewFile()
    {
        const string oldAvatarUrl = "http://storage.test/culinary-blog/avatars/user-1/old.png";
        const string newAvatarUrl = "http://storage.test/culinary-blog/avatars/user-1/new.png";

        var user = new ApplicationUser
        {
            Id = "user-1",
            AvatarUrl = oldAvatarUrl,
            UserName = "testuser",
            Email = "test@example.com"
        };

        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Simulate Identity update failure
        userManagerMock
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency error" }));

        var storageMock = new Mock<IFileStorageService>();
        storageMock
            .Setup(s => s.IsOwnedUrl(oldAvatarUrl))
            .Returns(true);

        storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile(newAvatarUrl));

        var deletionQueueMock = new Mock<IFileDeletionQueue>();

        var controller = new AvatarsController(
            userManagerMock.Object,
            storageMock.Object,
            deletionQueueMock.Object,
            NullLogger<AvatarsController>.Instance
        );

        var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aF3sAAAAASUVORK5CYII=");
        using var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, content.Length, "file", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var request = new ImageUploadRequest { File = formFile };

        var result = await controller.Upload(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
        user.AvatarUrl.Should().Be(oldAvatarUrl);
        deletionQueueMock.Verify(d => d.Enqueue(newAvatarUrl), Times.Once);
    }
}
