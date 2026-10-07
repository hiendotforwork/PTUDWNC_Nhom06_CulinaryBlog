using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class CategoryTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CategoryListAndDetails_OnlyExposePublishedRecipesToGuests()
    {
        var category = new Category
        {
            Name = "Món " + Guid.NewGuid().ToString("N")[..8],
            Slug = "mon-" + Guid.NewGuid().ToString("N"),
            OrderIndex = 1
        };
        var authorSession = await CreateAuthorClientAsync();
        using var author = authorSession.Client;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Categories.Add(category);
            db.Recipes.AddRange(
                CreateRecipe(category.Id, authorSession.UserId, RecipeStatus.Published, "published"),
                CreateRecipe(category.Id, authorSession.UserId, RecipeStatus.Draft, "draft"));
            await db.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<IMemoryCache>().Remove("categories:all");
        }

        using var client = factory.CreateClient();
        var listResponse = await client.GetAsync("/api/v1/categories");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await listResponse.Content.ReadFromJsonAsync<CategoryDto[]>();
        categories.Should().NotBeNull();
        categories!.Single(item => item.Id == category.Id).RecipeCount.Should().Be(1);

        var guestDetailResponse = await client.GetAsync($"/api/v1/categories/{category.Slug}?page=1&pageSize=1");
        guestDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var guestDetail = await guestDetailResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        guestDetail!.Recipes.TotalCount.Should().Be(1);
        guestDetail.Recipes.Items.Should().ContainSingle(item => item.Slug.EndsWith("-published"));
        guestDetail.Recipes.TotalPages.Should().Be(1);

        var authorDetailResponse = await author.GetAsync($"/api/v1/categories/{category.Slug}?page=1&pageSize=1");
        authorDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authorDetail = await authorDetailResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        authorDetail!.Recipes.TotalCount.Should().Be(2);
        authorDetail.Recipes.TotalPages.Should().Be(2);

        var missingResponse = await client.GetAsync("/api/v1/categories/category-does-not-exist");
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCategory_RequiresAdminGeneratesUniqueVietnameseSlugAndInvalidatesCache()
    {
        using var anonymous = factory.CreateClient();
        var unauthorizedResponse = await anonymous.PostAsJsonAsync(
            "/api/v1/categories", new { name = "Món ăn" });
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var author = (await CreateAuthorClientAsync()).Client;
        var forbiddenResponse = await author.PostAsJsonAsync(
            "/api/v1/categories", new { name = "Món ăn" });
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var admin = await CreateAdminClientAsync();
        var initialList = await admin.GetFromJsonAsync<CategoryDto[]>("/api/v1/categories");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createdResponse = await admin.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = $"Món ăn {suffix}", description = "Mô tả danh mục" });
        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdResponse.Headers.Location.Should().NotBeNull();
        var created = await createdResponse.Content.ReadFromJsonAsync<CategoryDto>();
        created!.Slug.Should().Be($"mon-an-{suffix}");
        created.RecipeCount.Should().Be(0);

        var duplicateSlugResponse = await admin.PostAsJsonAsync(
            "/api/v1/categories", new { name = $"Mon an! {suffix}" });
        duplicateSlugResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicateSlug = await duplicateSlugResponse.Content.ReadFromJsonAsync<CategoryDto>();
        duplicateSlug!.Slug.Should().Be($"mon-an-{suffix}-2");

        var updatedList = await admin.GetFromJsonAsync<CategoryDto[]>("/api/v1/categories");
        updatedList!.Length.Should().BeGreaterThan(initialList!.Length);

        var invalidResponse = await admin.PostAsJsonAsync(
            "/api/v1/categories", new { name = "<b>Danh mục</b>" });
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var missingNameResponse = await admin.PostAsJsonAsync(
            "/api/v1/categories", new { name = created.Name });
        missingNameResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<(HttpClient Client, string UserId)> CreateAuthorClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"category_author_{suffix}@culinaryblog.vn",
            userName = $"category_author_{suffix}",
            displayName = "Category Author",
            password = "Password123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, auth.User.Id);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"category_admin_{suffix}",
            Email = $"category_admin_{suffix}@culinaryblog.vn",
            DisplayName = "Category Admin"
        };

        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
            (await roleManager.CreateAsync(new IdentityRole("Admin"))).Succeeded.Should().BeTrue();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        (await userManager.CreateAsync(user, "Password123!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, "Admin")).Succeeded.Should().BeTrue();

        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenService.GenerateAccessToken(user, ["Admin"]));
        return client;
    }

    private static Recipe CreateRecipe(
        Guid categoryId,
        string authorId,
        RecipeStatus status,
        string slugSuffix)
    {
        return new Recipe
        {
            Title = $"Recipe {slugSuffix}",
            Slug = $"category-test-{Guid.NewGuid():N}-{slugSuffix}",
            Description = "Recipe for category endpoint test",
            CategoryId = categoryId,
            AuthorId = authorId,
            PrepTime = 10,
            CookTime = 20,
            Servings = 2,
            Difficulty = RecipeDifficulty.Easy,
            Status = status,
            PublishedAt = status == RecipeStatus.Published ? DateTimeOffset.UtcNow : null
        };
    }
}
