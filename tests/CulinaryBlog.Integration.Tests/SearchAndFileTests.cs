using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class SearchAndFileTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("/api/v1/recipes?page=abc")]
    [InlineData("/api/v1/recipes?pageSize=51")]
    [InlineData("/api/v1/recipes?page=2147483647")]
    [InlineData("/api/v1/recipes?difficulty=99")]
    [InlineData("/api/v1/recipes?sort=invalid")]
    [InlineData("/api/v1/recipes/search?q=pho&page=no")]
    [InlineData("/api/v1/recipes/search?q=!!")]
    [InlineData("/api/v1/recipes/search?q=pho&page=2147483647")]
    public async Task InvalidQueriesReturn422(string url)
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task ListRouteIsUnambiguous()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/recipes")).StatusCode);
    }

    [Fact]
    public async Task AvatarRequiresAuthentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/v1/users/me/avatar")).StatusCode);
    }

    [Fact]
    public async Task AvatarUploadReplacementAndDeletePersistMetadataAndQueueOldFiles()
    {
        using var client = factory.CreateClient();
        var auth = await Register(client);
        using var form = ImageForm();
        var response = await client.PostAsync("/api/v1/users/me/avatar", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var url = (await db.Users.AsNoTracking().SingleAsync(x => x.Id == auth.User.Id)).AvatarUrl!;
        Assert.Contains("/avatars/" + auth.User.Id + "/", url);
        using var replacement = ImageForm();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/v1/users/me/avatar", replacement)).StatusCode);
        var storage = (TestFileStorage)scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        Assert.Contains(url, storage.Deletions);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/users/me/avatar")).StatusCode);
        Assert.Null((await db.Users.AsNoTracking().SingleAsync(x => x.Id == auth.User.Id)).AvatarUrl);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/users/me/avatar")).StatusCode);
    }

    [Fact]
    public async Task AvatarRejectsForgedMime()
    {
        using var client = factory.CreateClient();
        await Register(client);
        using var form = ImageForm("image/jpeg");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync("/api/v1/users/me/avatar", form)).StatusCode);
    }

    [Fact]
    public async Task RecipeImageUploadChecksOwnership()
    {
        using var owner = factory.CreateClient();
        using var other = factory.CreateClient();
        var auth = await Register(owner);
        await Register(other);
        var id = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var category = new Category { Name = "Test", Slug = Guid.NewGuid().ToString("N") };
            db.Categories.Add(category);
            db.Recipes.Add(new Recipe { Id = id, Title = "Test recipe", Slug = id.ToString(), AuthorId = auth.User.Id, CategoryId = category.Id });
            await db.SaveChangesAsync();
        }
        using var forbidden = ImageForm();
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync($"/api/v1/recipes/{id}/images", forbidden)).StatusCode);
        using var allowed = ImageForm();
        var uploaded = await owner.PostAsync($"/api/v1/recipes/{id}/images", allowed);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        var image = await uploaded.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var imageId = image.GetProperty("id").GetGuid();
        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/recipes/{id}/images/{imageId}")
        {
            Content = JsonContent.Create(new { rowVersion = image.GetProperty("rowVersion").GetString() })
        };
        Assert.Equal(HttpStatusCode.NoContent, (await owner.SendAsync(delete)).StatusCode);
        using var checkScope = factory.Services.CreateScope();
        var storage = (TestFileStorage)checkScope.ServiceProvider.GetRequiredService<IFileStorageService>();
        Assert.Contains(image.GetProperty("originalUrl").GetString()!, storage.Deletions);
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await checkDb.RecipeImages.AnyAsync(x => x.Id == imageId));
    }

    private static MultipartFormDataContent ImageForm(string mime = "image/png")
    {
        var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aF3sAAAAASUVORK5CYII="));
        image.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(image, "file", "pixel.png");
        return form;
    }

    private static async Task<AuthResponse> Register(HttpClient client)
    {
        var name = "file" + Guid.NewGuid().ToString("N")[..12];
        var result = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(name + "@example.com", name, name, "Password123!"));
        result.EnsureSuccessStatusCode();
        var auth = (await result.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }
}
