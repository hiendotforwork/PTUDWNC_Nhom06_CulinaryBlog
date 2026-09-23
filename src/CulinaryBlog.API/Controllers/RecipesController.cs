// Tệp này cung cấp API cho vòng đời chính của công thức nấu ăn.
// Chức năng: xem danh sách (GetAll), xem chi tiết (GetBySlug), tạo (Create), cập nhật (Update),
// xuất bản/hủy xuất bản (Publish, Unpublish), lưu trữ/khôi phục (Archive, Unarchive) và xóa mềm (Delete).

namespace CulinaryBlog.API.Controllers;

using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CulinaryBlog.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/v1/recipes")]
// Class điều phối các endpoint FR-RCP-001 đến FR-RCP-007.
// Input: ApplicationDbContext. Output: phản hồi HTTP chứa dữ liệu hoặc lỗi nghiệp vụ.
public sealed class RecipesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    // Chức năng: lấy danh sách công thức có phân trang, lọc và sắp xếp.
    // Input: page, pageSize, search, categoryId, difficulty, sort, mine và cancellationToken.
    // Output: RecipePageResponse hoặc lỗi xác thực/tham số.
    public async Task<ActionResult<RecipePageResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] RecipeDifficulty? difficulty = null,
        [FromQuery] bool mine = false,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new { errorCode = "INVALID_PAGINATION", message = "page phải >= 1 và pageSize từ 1 đến 100." });

        var userId = CurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        if (mine && userId is null) return Unauthorized();

        var recipes = db.Recipes.AsNoTracking().AsQueryable();
        recipes = mine
            ? (isAdmin ? recipes : recipes.Where(x => x.AuthorId == userId))
            : recipes.Where(x => x.Status == RecipeStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            recipes = recipes.Where(x => EF.Functions.ILike(x.Title, term) || EF.Functions.ILike(x.Description, term));
        }
        if (categoryId.HasValue) recipes = recipes.Where(x => x.CategoryId == categoryId);
        if (difficulty.HasValue) recipes = recipes.Where(x => x.Difficulty == difficulty);

        var descending = !string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        recipes = sortBy.ToLowerInvariant() switch
        {
            "title" => descending ? recipes.OrderByDescending(x => x.Title) : recipes.OrderBy(x => x.Title),
            "publishedat" => descending ? recipes.OrderByDescending(x => x.PublishedAt) : recipes.OrderBy(x => x.PublishedAt),
            "preptime" => descending ? recipes.OrderByDescending(x => x.PrepTime) : recipes.OrderBy(x => x.PrepTime),
            "cooktime" => descending ? recipes.OrderByDescending(x => x.CookTime) : recipes.OrderBy(x => x.CookTime),
            "createdat" => descending ? recipes.OrderByDescending(x => x.CreatedAt) : recipes.OrderBy(x => x.CreatedAt),
            _ => throw new BadHttpRequestException("sortBy không hợp lệ.")
        };

        var totalCount = await recipes.CountAsync(cancellationToken);
        var items = await (from recipe in recipes
                           join author in db.Users.AsNoTracking() on recipe.AuthorId equals author.Id
                           select new RecipeListItem(
                               recipe.Id, recipe.Title, recipe.Slug, recipe.Description,
                               recipe.PrepTime, recipe.CookTime, recipe.Servings,
                               recipe.Difficulty, recipe.Status, recipe.PublishedAt,
                               recipe.CategoryId, recipe.Category.Name,
                               recipe.AuthorId, author.DisplayName,
                               recipe.Images.Where(i => i.IsPrimary)
                                   .Select(i => i.ThumbnailUrl ?? i.MediumUrl ?? i.OriginalUrl).FirstOrDefault(),
                               recipe.CreatedAt, Convert.ToBase64String(recipe.RowVersion)))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new RecipePageResponse(items, totalCount, page, pageSize, totalPages, page < totalPages, page > 1));
    }

    [HttpGet("{slug}")]
    // Chức năng: lấy chi tiết công thức được phép xem theo slug.
    // Input: slug và cancellationToken. Output: RecipeDetailResponse hoặc 404.
    public async Task<ActionResult<RecipeDetailResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.AsNoTracking().AsSplitQuery()
            .Include(x => x.Category).Include(x => x.Ingredients)
            .Include(x => x.Steps).Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        if (recipe is null) return NotFound();

        var userId = CurrentUserId();
        if (recipe.Status != RecipeStatus.Published && recipe.AuthorId != userId && !User.IsInRole("Admin"))
            return NotFound();

        var authorName = await db.Users.AsNoTracking().Where(x => x.Id == recipe.AuthorId)
            .Select(x => x.DisplayName).SingleAsync(cancellationToken);
        return Ok(new RecipeDetailResponse(
            recipe.Id, recipe.Title, recipe.Slug, recipe.Description, recipe.Instructions,
            recipe.PrepTime, recipe.CookTime, recipe.Servings, recipe.Difficulty, recipe.Status,
            recipe.PublishedAt, recipe.CategoryId, recipe.Category.Name, recipe.AuthorId, authorName,
            recipe.Nutrition,
            recipe.Ingredients.OrderBy(x => x.OrderIndex).Select(x => new RecipeIngredientItem(x.Id, x.Name, x.Quantity, x.Unit, x.Notes, x.OrderIndex, Convert.ToBase64String(x.RowVersion))),
            recipe.Steps.OrderBy(x => x.StepNumber).Select(x => new RecipeStepItem(x.Id, x.StepNumber, x.Title, x.Description, x.TimerMinutes, x.ImageUrl, Convert.ToBase64String(x.RowVersion))),
            recipe.Images.OrderBy(x => x.OrderIndex).Select(x => new RecipeImageItem(x.Id, x.OriginalUrl, x.MediumUrl, x.ThumbnailUrl, x.AltText, x.IsPrimary, x.OrderIndex, Convert.ToBase64String(x.RowVersion))),
            Convert.ToBase64String(recipe.RowVersion), recipe.CreatedAt, recipe.UpdatedAt));
    }
    [HttpPost]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: tạo một công thức ở trạng thái nháp cho Author/Admin.
    // Input: CreateRecipeRequest và cancellationToken. Output: 201 kèm RecipeMutationResponse hoặc lỗi kiểm tra.
    public async Task<IActionResult> Create(CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request.Title, request.Description, request.PrepTime, request.CookTime, request.Servings, request.Difficulty, request.CategoryId, cancellationToken);
        if (validation is not null) return UnprocessableEntity(validation);

        var slugBase = Slugify(request.Title);
        var slug = slugBase;
        var suffix = 2;
        while (await db.Recipes.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug, cancellationToken))
            slug = $"{slugBase}-{suffix++}";

        var recipe = new Recipe
        {
            Title = request.Title.Trim(), Slug = slug, Description = request.Description.Trim(),
            Instructions = request.Instructions?.Trim() ?? "", PrepTime = request.PrepTime,
            CookTime = request.CookTime, Servings = request.Servings, Difficulty = request.Difficulty,
            CategoryId = request.CategoryId, AuthorId = CurrentUserId()!, Status = RecipeStatus.Draft,
            Nutrition = request.Nutrition ?? new RecipeNutrition()
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = recipe.Slug },
            new RecipeMutationResponse(recipe.Id, recipe.Slug, recipe.Status, Convert.ToBase64String(recipe.RowVersion)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: cập nhật thông tin chính của công thức theo quyền sở hữu và RowVersion.
    // Input: id, UpdateRecipeRequest và cancellationToken. Output: RecipeMutationResponse, 403, 404, 409 hoặc 422.
    public async Task<IActionResult> Update(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken)
    {
        byte[] expectedVersion;
        try { expectedVersion = Convert.FromBase64String(request.RowVersion); }
        catch (FormatException) { return UnprocessableEntity(new { errorCode = "INVALID_ROW_VERSION", message = "RowVersion phải là Base64 hợp lệ." }); }
        if (expectedVersion.Length != 16)
            return UnprocessableEntity(new { errorCode = "INVALID_ROW_VERSION", message = "RowVersion không đúng định dạng." });

        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (recipe is null) return NotFound();
        if (recipe.AuthorId != CurrentUserId() && !User.IsInRole(AppRoles.Admin)) return Forbid();

        var validation = await ValidateRequest(request.Title, request.Description, request.PrepTime, request.CookTime, request.Servings, request.Difficulty, request.CategoryId, cancellationToken);
        if (validation is not null) return UnprocessableEntity(validation);

        db.Entry(recipe).Property(x => x.RowVersion).OriginalValue = expectedVersion;
        recipe.Title = request.Title.Trim();
        recipe.Description = request.Description.Trim();
        recipe.Instructions = request.Instructions?.Trim() ?? "";
        recipe.PrepTime = request.PrepTime; recipe.CookTime = request.CookTime;
        recipe.Servings = request.Servings; recipe.Difficulty = request.Difficulty;
        recipe.CategoryId = request.CategoryId; recipe.Nutrition = request.Nutrition ?? new RecipeNutrition();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            var latest = await db.Recipes.AsNoTracking().Where(x => x.Id == id).Select(x => x.RowVersion).SingleOrDefaultAsync(cancellationToken);
            return Conflict(new { errorCode = "RECIPE_CONCURRENCY_CONFLICT", message = "Công thức đã được thay đổi bởi người khác.", rowVersion = latest is null ? null : Convert.ToBase64String(latest) });
        }
        return Ok(new RecipeMutationResponse(recipe.Id, recipe.Slug, recipe.Status, Convert.ToBase64String(recipe.RowVersion)));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: xuất bản công thức đã có ít nhất một nguyên liệu và một bước.
    // Input: id, VersionRequest và cancellationToken. Output: trạng thái mới hoặc lỗi nghiệp vụ.
    public async Task<IActionResult> Publish(Guid id, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(id, ct); if (access.Result is not null) return access.Result;
        var recipe = access.Value!;
        if (recipe.Status == RecipeStatus.Archived)
            return UnprocessableEntity(BusinessError("ARCHIVED_RECIPE", "Phải khôi phục công thức trước khi xuất bản."));
        if (!await db.RecipeIngredients.AnyAsync(x => x.RecipeId == id, ct) || !await db.RecipeSteps.AnyAsync(x => x.RecipeId == id, ct))
            return UnprocessableEntity(BusinessError("RECIPE_NOT_READY", "Công thức cần ít nhất một nguyên liệu và một bước thực hiện."));
        return await ChangeStatus(recipe, request.RowVersion, RecipeStatus.Published, DateTimeOffset.UtcNow, ct);
    }

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: đưa công thức đã xuất bản về bản nháp.
    // Input: id, VersionRequest và cancellationToken. Output: trạng thái mới hoặc lỗi nghiệp vụ.
    public async Task<IActionResult> Unpublish(Guid id, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(id, ct); if (access.Result is not null) return access.Result;
        if (access.Value!.Status != RecipeStatus.Published)
            return UnprocessableEntity(BusinessError("INVALID_RECIPE_STATUS", "Chỉ công thức Published mới có thể hủy xuất bản."));
        return await ChangeStatus(access.Value, request.RowVersion, RecipeStatus.Draft, null, ct);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: lưu trữ công thức.
    // Input: id, VersionRequest và cancellationToken. Output: trạng thái Archived hoặc lỗi.
    public async Task<IActionResult> Archive(Guid id, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(id, ct); if (access.Result is not null) return access.Result;
        if (access.Value!.Status == RecipeStatus.Archived)
            return UnprocessableEntity(BusinessError("INVALID_RECIPE_STATUS", "Công thức đã được lưu trữ."));
        return await ChangeStatus(access.Value, request.RowVersion, RecipeStatus.Archived, null, ct);
    }

    [HttpPost("{id:guid}/unarchive")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: khôi phục công thức lưu trữ về bản nháp.
    // Input: id, VersionRequest và cancellationToken. Output: trạng thái Draft hoặc lỗi.
    public async Task<IActionResult> Unarchive(Guid id, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(id, ct); if (access.Result is not null) return access.Result;
        if (access.Value!.Status != RecipeStatus.Archived)
            return UnprocessableEntity(BusinessError("INVALID_RECIPE_STATUS", "Chỉ công thức Archived mới có thể khôi phục."));
        return await ChangeStatus(access.Value, request.RowVersion, RecipeStatus.Draft, null, ct);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
    // Chức năng: xóa mềm công thức và các thành phần con.
    // Input: id, VersionRequest và cancellationToken. Output: 204 hoặc lỗi quyền/đồng thời.
    public async Task<IActionResult> Delete(Guid id, VersionRequest request, CancellationToken ct)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters().Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (recipe is null) return NotFound();
        if (recipe.AuthorId != CurrentUserId() && !User.IsInRole(AppRoles.Admin)) return Forbid();
        if (!TryRowVersion(request.RowVersion, out var version, out var error)) return error!;
        db.Entry(recipe).Property(x => x.RowVersion).OriginalValue = version!;
        recipe.IsDeleted = true;
        foreach (var item in recipe.Ingredients) item.IsDeleted = true;
        foreach (var item in recipe.Steps) item.IsDeleted = true;
        foreach (var item in recipe.Images) item.IsDeleted = true;
        try { await db.SaveChangesAsync(ct); return NoContent(); }
        catch (DbUpdateConcurrencyException) { return Conflict(ConcurrencyError()); }
    }

    // Chức năng: tìm công thức và kiểm tra quyền sửa của owner/Admin.
    // Input: id và cancellationToken. Output: công thức hoặc IActionResult lỗi.
    private async Task<(Recipe? Value, IActionResult? Result)> EditableRecipe(Guid id, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (recipe is null) return (null, NotFound());
        return recipe.AuthorId == CurrentUserId() || User.IsInRole(AppRoles.Admin) ? (recipe, null) : (null, Forbid());
    }

    // Chức năng: thay đổi trạng thái có kiểm tra RowVersion.
    // Input: recipe, rowVersion, status, publishedAt và cancellationToken. Output: trạng thái mới hoặc 409.
    private async Task<IActionResult> ChangeStatus(Recipe recipe, string rowVersion, RecipeStatus status, DateTimeOffset? publishedAt, CancellationToken ct)
    {
        if (!TryRowVersion(rowVersion, out var version, out var error)) return error!;
        db.Entry(recipe).Property(x => x.RowVersion).OriginalValue = version!;
        recipe.Status = status; recipe.PublishedAt = publishedAt;
        try
        {
            await db.SaveChangesAsync(ct);
            return Ok(new RecipeMutationResponse(recipe.Id, recipe.Slug, recipe.Status, Convert.ToBase64String(recipe.RowVersion)));
        }
        catch (DbUpdateConcurrencyException) { return Conflict(ConcurrencyError()); }
    }

    // Chức năng: giải mã và kiểm tra RowVersion Base64.
    // Input: text. Output: true kèm byte[] hợp lệ hoặc IActionResult lỗi.
    private bool TryRowVersion(string text, out byte[]? version, out IActionResult? error)
    {
        try { version = Convert.FromBase64String(text); if (version.Length == 16) { error = null; return true; } } catch (FormatException) { }
        version = null; error = UnprocessableEntity(new { errorCode = "INVALID_ROW_VERSION", message = "RowVersion không hợp lệ." }); return false;
    }
    // Chức năng: tạo payload lỗi nghiệp vụ thống nhất.
    // Input: code và message. Output: object lỗi.
    private static object BusinessError(string code, string message) => new { errorCode = code, message };
    // Chức năng: tạo payload lỗi xung đột cập nhật.
    // Input: không có. Output: object lỗi đồng thời.
    private static object ConcurrencyError() => new { errorCode = "RECIPE_CONCURRENCY_CONFLICT", message = "Công thức đã được thay đổi bởi người khác." };
    // Chức năng: đọc mã người dùng hiện tại từ JWT claims.
    // Input: claims của request. Output: userId hoặc null.
    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    // Chức năng: kiểm tra dữ liệu công thức và sự tồn tại của danh mục.
    // Input: các trường công thức, categoryId và cancellationToken. Output: null nếu hợp lệ hoặc object lỗi.
    private async Task<object?> ValidateRequest(string title, string description, int prepTime, int cookTime, int servings, RecipeDifficulty difficulty, Guid categoryId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 5 or > 200) errors["title"] = ["Tiêu đề phải có từ 5 đến 200 ký tự."];
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000) errors["description"] = ["Mô tả phải có từ 1 đến 2000 ký tự."];
        if (prepTime <= 0) errors["prepTime"] = ["Thời gian chuẩn bị phải lớn hơn 0."];
        if (cookTime < 0) errors["cookTime"] = ["Thời gian nấu không được âm."];
        if (servings <= 0) errors["servings"] = ["Khẩu phần phải lớn hơn 0."];
        if (!Enum.IsDefined(difficulty)) errors["difficulty"] = ["Độ khó không hợp lệ."];
        if (!await db.Categories.AnyAsync(x => x.Id == categoryId, cancellationToken)) errors["categoryId"] = ["Danh mục không tồn tại."];
        return errors.Count == 0 ? null : new { errorCode = "VALIDATION_ERROR", message = "Dữ liệu không hợp lệ.", errors };
    }

    // Chức năng: chuẩn hóa tiêu đề thành slug URL.
    // Input: value - chuỗi tiêu đề. Output: slug chữ thường chỉ gồm ký tự hợp lệ.
    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) builder.Append(ch == 'đ' ? 'd' : ch);
        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), "[^a-z0-9]+", "-").Trim('-');
    }
}

public sealed record RecipePageResponse(IReadOnlyList<RecipeListItem> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasNextPage, bool HasPreviousPage);
public sealed record RecipeListItem(Guid Id, string Title, string Slug, string Description, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, RecipeStatus Status, DateTimeOffset? PublishedAt, Guid CategoryId, string CategoryName, string AuthorId, string AuthorName, string? PrimaryImageUrl, DateTimeOffset CreatedAt, string RowVersion);
public sealed record RecipeIngredientItem(Guid Id, string Name, decimal? Quantity, string? Unit, string? Notes, int OrderIndex, string RowVersion);
public sealed record RecipeStepItem(Guid Id, int StepNumber, string Title, string Description, int? TimerMinutes, string? ImageUrl, string RowVersion);
public sealed record RecipeImageItem(Guid Id, string OriginalUrl, string? MediumUrl, string? ThumbnailUrl, string? AltText, bool IsPrimary, int OrderIndex, string RowVersion);
public sealed record RecipeDetailResponse(Guid Id, string Title, string Slug, string Description, string Instructions, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, RecipeStatus Status, DateTimeOffset? PublishedAt, Guid CategoryId, string CategoryName, string AuthorId, string AuthorName, RecipeNutrition Nutrition, IEnumerable<RecipeIngredientItem> Ingredients, IEnumerable<RecipeStepItem> Steps, IEnumerable<RecipeImageItem> Images, string RowVersion, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
public sealed record CreateRecipeRequest(string Title, string Description, string? Instructions, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, Guid CategoryId, RecipeNutrition? Nutrition);
public sealed record UpdateRecipeRequest(string Title, string Description, string? Instructions, int PrepTime, int CookTime, int Servings, RecipeDifficulty Difficulty, Guid CategoryId, RecipeNutrition? Nutrition, string RowVersion);
public sealed record RecipeMutationResponse(Guid Id, string Slug, RecipeStatus Status, string RowVersion);
