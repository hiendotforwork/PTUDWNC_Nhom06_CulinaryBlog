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

        var deletedSlugSuffix = Guid.NewGuid().ToString("N")[..6];
        var deletedName = $"Deleted category {deletedSlugSuffix}";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Categories.Add(new Category
            {
                Name = deletedName,
                Slug = $"deleted-category-{deletedSlugSuffix}",
                IsDeleted = true,
                OrderIndex = 1
            });
            await db.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<IMemoryCache>().Remove("categories:all");
        }

        var recreatedResponse = await admin.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = deletedName, description = (string?)null });
        recreatedResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var recreated = await recreatedResponse.Content.ReadFromJsonAsync<CategoryDto>();
        recreated!.Slug.Should().Be($"deleted-category-{deletedSlugSuffix}-2");
    }

    [Fact]
    public async Task UpdateCategory_ChangesNameAndDescriptionButPreservesSlug()
    {
        var category = await CreateCategoryAsync();
        using var anonymous = factory.CreateClient();
        var unauthorizedResponse = await anonymous.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "Không được cập nhật", description = (string?)null });
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var author = (await CreateAuthorClientAsync()).Client;
        var forbiddenResponse = await author.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "Không được cập nhật", description = (string?)null });
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var admin = await CreateAdminClientAsync();
        await admin.GetAsync("/api/v1/categories");

        var response = await admin.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "Tên mới " + Guid.NewGuid().ToString("N")[..6], description = "Mô tả mới" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().StartWith("Tên mới ");
        updated.Description.Should().Be("Mô tả mới");
        updated.Slug.Should().Be(category.Slug);

        var detailResponse = await admin.GetAsync($"/api/v1/categories/{category.Slug}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<CategoryDetailDto>();
        detail!.Category.Name.Should().Be(updated.Name);

        var refreshedList = await admin.GetFromJsonAsync<CategoryDto[]>("/api/v1/categories");
        refreshedList!.Single(item => item.Id == category.Id).Name.Should().Be(updated.Name);

        var missingResponse = await admin.PutAsJsonAsync(
            $"/api/v1/categories/{Guid.NewGuid()}",
            new { name = "Tên hợp lệ", description = (string?)null });
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var invalidResponse = await admin.PutAsJsonAsync(
            $"/api/v1/categories/{category.Id}",
            new { name = "<b>Không hợp lệ</b>", description = (string?)null });
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteCategory_OnlyDeletesEmptyCategoriesAndRejectsCategoriesWithDraftRecipes()
    {
        var emptyCategory = await CreateCategoryAsync();
        using var anonymous = factory.CreateClient();
        (await anonymous.DeleteAsync($"/api/v1/categories/{emptyCategory.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var author = (await CreateAuthorClientAsync()).Client;
        (await author.DeleteAsync($"/api/v1/categories/{emptyCategory.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var admin = await CreateAdminClientAsync();
        await admin.GetAsync("/api/v1/categories");

        var deletedResponse = await admin.DeleteAsync($"/api/v1/categories/{emptyCategory.Id}");
        deletedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedDetailResponse = await admin.GetAsync($"/api/v1/categories/{emptyCategory.Slug}");
        deletedDetailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var categoriesAfterDelete = await admin.GetFromJsonAsync<CategoryDto[]>("/api/v1/categories");
        categoriesAfterDelete!.Should().NotContain(category => category.Id == emptyCategory.Id);

        var missingResponse = await admin.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var categoryWithDraft = await CreateCategoryAsync();
        var authorSession = await CreateAuthorClientAsync();
        authorSession.Client.Dispose();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Recipes.Add(CreateRecipe(
                categoryWithDraft.Id,
                authorSession.UserId,
                RecipeStatus.Draft,
                "draft"));
            await db.SaveChangesAsync();
        }

        var conflictResponse = await admin.DeleteAsync($"/api/v1/categories/{categoryWithDraft.Id}");
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await conflictResponse.Content.ReadAsStringAsync()).Should().Contain("1 công thức");

        var categoryStillExists = await admin.GetAsync($"/api/v1/categories/{categoryWithDraft.Slug}");
        categoryStillExists.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Category> CreateCategoryAsync()
    {
        var category = new Category
        {
            Name = "Danh mục test " + Guid.NewGuid().ToString("N")[..8],
            Slug = "category-test-" + Guid.NewGuid().ToString("N"),
            OrderIndex = 1
        };
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        scope.ServiceProvider.GetRequiredService<IMemoryCache>().Remove("categories:all");
        return category;
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
