// Tệp này kiểm thử tích hợp vòng đời chính của FR-RCP qua HTTP.
// Chức năng: tạo công thức, thêm nguyên liệu/bước, xuất bản và kiểm tra quyền sở hữu.

namespace CulinaryBlog.Integration.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.API.Controllers;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class RecipeLifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RecipeLifecycleTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    // Chức năng: xác nhận Author có thể hoàn thành luồng tạo đến xuất bản.
    // Input: API chạy với database kiểm thử. Output: các phản hồi 201/200 và trạng thái Published.
    public async Task Author_CanCreateComponentsAndPublishRecipe()
    {
        var categoryId = await CreateCategoryAsync();
        using var client = await CreateAuthorClientAsync("lifecycle");

        var created = await CreateRecipeAsync(client, categoryId, "Bún bò kiểm thử");

        var ingredientResponse = await client.PostAsJsonAsync($"/api/v1/recipes/{created.Id}/ingredients",
            new IngredientRequest("Thịt bò", 500, "g", null));
        ingredientResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var stepResponse = await client.PostAsJsonAsync($"/api/v1/recipes/{created.Id}/steps",
            new StepRequest("Nấu nước dùng", "Hầm xương và nêm gia vị.", 60, null));
        stepResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var publishResponse = await client.PostAsJsonAsync($"/api/v1/recipes/{created.Id}/publish",
            new VersionRequest(created.RowVersion));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var published = await publishResponse.Content.ReadFromJsonAsync<RecipeMutationResponse>(_jsonOptions);
        published.Should().NotBeNull();
        published!.Status.Should().Be(RecipeStatus.Published);

        var detailResponse = await client.GetAsync($"/api/v1/recipes/{created.Slug}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailResponse.Content.ReadFromJsonAsync<RecipeDetailResponse>(_jsonOptions);
        detail!.Ingredients.Should().ContainSingle();
        detail.Steps.Should().ContainSingle();
    }

    [Fact]
    // Chức năng: xác nhận công thức thiếu thành phần không được xuất bản.
    // Input: công thức nháp chưa có nguyên liệu/bước. Output: HTTP 422 với mã RECIPE_NOT_READY.
    public async Task Publish_WithoutIngredientsAndSteps_ReturnsRecipeNotReady()
    {
        var categoryId = await CreateCategoryAsync();
        using var client = await CreateAuthorClientAsync("notready");
        var created = await CreateRecipeAsync(client, categoryId, "Món ăn chưa hoàn chỉnh");

        var response = await client.PostAsJsonAsync($"/api/v1/recipes/{created.Id}/publish",
            new VersionRequest(created.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync()).Should().Contain("RECIPE_NOT_READY");
    }

    [Fact]
    // Chức năng: xác nhận Author khác không thể sửa công thức không thuộc sở hữu.
    // Input: công thức của Author thứ nhất và token Author thứ hai. Output: HTTP 403.
    public async Task Update_ByAnotherAuthor_ReturnsForbidden()
    {
        var categoryId = await CreateCategoryAsync();
        using var owner = await CreateAuthorClientAsync("owner");
        var created = await CreateRecipeAsync(owner, categoryId, "Công thức của chủ sở hữu");
        using var otherAuthor = await CreateAuthorClientAsync("other");

        var request = new UpdateRecipeRequest(
            "Cố sửa công thức", "Nội dung không được phép sửa", null,
            10, 20, 2, RecipeDifficulty.Easy, categoryId, null, created.RowVersion);
        var response = await otherAuthor.PutAsJsonAsync($"/api/v1/recipes/{created.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateCategoryAsync()
    {
        var category = new Category
        {
            Name = "Danh mục " + Guid.NewGuid().ToString("N")[..8],
            Slug = "test-" + Guid.NewGuid().ToString("N"),
            OrderIndex = 1
        };
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task<HttpClient> CreateAuthorClientAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"{prefix}_{suffix}@culinaryblog.vn",
            userName = $"{prefix}_{suffix}",
            displayName = $"Chef {prefix}",
            password = "Password123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private async Task<RecipeMutationResponse> CreateRecipeAsync(HttpClient client, Guid categoryId, string title)
    {
        var response = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeRequest(
            title, "Mô tả công thức dùng trong kiểm thử tích hợp.", null,
            15, 30, 4, RecipeDifficulty.Medium, categoryId, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RecipeMutationResponse>(_jsonOptions))!;
    }
}
