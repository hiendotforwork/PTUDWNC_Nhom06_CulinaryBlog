// Tệp này cung cấp API quản lý nguyên liệu và các bước thực hiện của công thức.
// Chức năng: CRUD nguyên liệu (AddIngredient, UpdateIngredient, DeleteIngredient) và
// CRUD bước làm (AddStep, UpdateStep, DeleteStep).

namespace CulinaryBlog.API.Controllers;

using System.Security.Claims;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
[Route("api/v1/recipes/{recipeId:guid}")]
// Class điều phối FR-RCP-009 và FR-RCP-010.
// Input: ApplicationDbContext. Output: phản hồi HTTP cho thao tác thành phần công thức.
public sealed class RecipeComponentsController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("ingredients")]
    // Chức năng: thêm nguyên liệu vào cuối danh sách.
    // Input: recipeId, IngredientRequest và cancellationToken. Output: nguyên liệu đã tạo hoặc lỗi.
    public async Task<IActionResult> AddIngredient(Guid recipeId, IngredientRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct);
        if (recipe.Result is not null) return recipe.Result;
        var error = ValidateIngredient(request.Name, request.Quantity, request.Unit);
        if (error is not null) return UnprocessableEntity(error);
        var order = await db.RecipeIngredients.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.OrderIndex).MaxAsync(ct) ?? -1;
        var item = new RecipeIngredient { RecipeId = recipeId, Name = request.Name.Trim(), Quantity = request.Quantity, Unit = request.Unit?.Trim(), Notes = request.Notes?.Trim(), OrderIndex = order + 1 };
        db.RecipeIngredients.Add(item); await db.SaveChangesAsync(ct);
        return StatusCode(201, IngredientResponse(item));
    }

    [HttpPut("ingredients/{id:guid}")]
    // Chức năng: cập nhật nguyên liệu và thứ tự hiển thị.
    // Input: recipeId, id, IngredientUpdateRequest và cancellationToken. Output: nguyên liệu mới hoặc lỗi.
    public async Task<IActionResult> UpdateIngredient(Guid recipeId, Guid id, IngredientUpdateRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct); if (recipe.Result is not null) return recipe.Result;
        var item = await db.RecipeIngredients.SingleOrDefaultAsync(x => x.Id == id && x.RecipeId == recipeId, ct); if (item is null) return NotFound();
        var error = ValidateIngredient(request.Name, request.Quantity, request.Unit); if (error is not null) return UnprocessableEntity(error);
        if (!TryVersion(request.RowVersion, out var version, out var versionError)) return versionError!;
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = version!;
        item.Name = request.Name.Trim(); item.Quantity = request.Quantity; item.Unit = request.Unit?.Trim(); item.Notes = request.Notes?.Trim(); item.OrderIndex = request.OrderIndex;
        if (item.OrderIndex < 0) return UnprocessableEntity(Validation("orderIndex", "Thứ tự không được âm."));
        return await SaveComponent(item.Id, () => IngredientResponse(item), ct);
    }

    [HttpDelete("ingredients/{id:guid}")]
    // Chức năng: xóa mềm một nguyên liệu.
    // Input: recipeId, id, VersionRequest và cancellationToken. Output: 204 hoặc lỗi.
    public async Task<IActionResult> DeleteIngredient(Guid recipeId, Guid id, VersionRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct); if (recipe.Result is not null) return recipe.Result;
        var item = await db.RecipeIngredients.SingleOrDefaultAsync(x => x.Id == id && x.RecipeId == recipeId, ct); if (item is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version, out var versionError)) return versionError!;
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = version!; item.IsDeleted = true;
        return await SaveComponent(item.Id, () => null, ct, noContent: true);
    }

    [HttpPost("steps")]
    // Chức năng: thêm bước thực hiện ở cuối danh sách.
    // Input: recipeId, StepRequest và cancellationToken. Output: bước đã tạo hoặc lỗi.
    public async Task<IActionResult> AddStep(Guid recipeId, StepRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct); if (recipe.Result is not null) return recipe.Result;
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000)
            return UnprocessableEntity(Validation("description", "Nội dung bước phải có từ 1 đến 2000 ký tự."));
        if (request.TimerMinutes < 0) return UnprocessableEntity(Validation("timerMinutes", "Thời gian không được âm."));
        var number = (await db.RecipeSteps.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.StepNumber).MaxAsync(ct) ?? 0) + 1;
        var item = new RecipeStep { RecipeId = recipeId, StepNumber = number, Title = string.IsNullOrWhiteSpace(request.Title) ? $"Bước {number}" : request.Title.Trim(), Description = request.Description.Trim(), TimerMinutes = request.TimerMinutes, ImageUrl = request.ImageUrl?.Trim() };
        db.RecipeSteps.Add(item); await db.SaveChangesAsync(ct);
        return StatusCode(201, StepResponse(item));
    }

    [HttpPut("steps/{id:guid}")]
    // Chức năng: cập nhật nội dung một bước thực hiện.
    // Input: recipeId, id, StepUpdateRequest và cancellationToken. Output: bước mới hoặc lỗi.
    public async Task<IActionResult> UpdateStep(Guid recipeId, Guid id, StepUpdateRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct); if (recipe.Result is not null) return recipe.Result;
        var item = await db.RecipeSteps.SingleOrDefaultAsync(x => x.Id == id && x.RecipeId == recipeId, ct); if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000) return UnprocessableEntity(Validation("description", "Nội dung bước phải có từ 1 đến 2000 ký tự."));
        if (request.TimerMinutes < 0) return UnprocessableEntity(Validation("timerMinutes", "Thời gian không được âm."));
        if (!TryVersion(request.RowVersion, out var version, out var versionError)) return versionError!;
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = version!;
        item.Title = string.IsNullOrWhiteSpace(request.Title) ? $"Bước {item.StepNumber}" : request.Title.Trim(); item.Description = request.Description.Trim(); item.TimerMinutes = request.TimerMinutes; item.ImageUrl = request.ImageUrl?.Trim();
        return await SaveComponent(item.Id, () => StepResponse(item), ct);
    }

    [HttpDelete("steps/{id:guid}")]
    // Chức năng: xóa mềm một bước thực hiện.
    // Input: recipeId, id, VersionRequest và cancellationToken. Output: 204 hoặc lỗi.
    public async Task<IActionResult> DeleteStep(Guid recipeId, Guid id, VersionRequest request, CancellationToken ct)
    {
        var recipe = await EditableRecipe(recipeId, ct); if (recipe.Result is not null) return recipe.Result;
        var item = await db.RecipeSteps.SingleOrDefaultAsync(x => x.Id == id && x.RecipeId == recipeId, ct); if (item is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version, out var versionError)) return versionError!;
        db.Entry(item).Property(x => x.RowVersion).OriginalValue = version!; item.IsDeleted = true;
        return await SaveComponent(item.Id, () => null, ct, noContent: true);
    }

    // Chức năng: kiểm tra công thức tồn tại và người dùng có quyền sửa.
    // Input: id và cancellationToken. Output: công thức hoặc IActionResult lỗi.
    private async Task<(Recipe? Value, IActionResult? Result)> EditableRecipe(Guid id, CancellationToken ct)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(x => x.Id == id, ct); if (recipe is null) return (null, NotFound());
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return recipe.AuthorId == userId || User.IsInRole(AppRoles.Admin) ? (recipe, null) : (null, Forbid());
    }
    // Chức năng: kiểm tra tên, số lượng và đơn vị nguyên liệu.
    // Input: name, quantity và unit. Output: null hoặc object lỗi.
    private static object? ValidateIngredient(string name, decimal? quantity, string? unit)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return Validation("name", "Tên nguyên liệu phải có từ 1 đến 200 ký tự.");
        if ((quantity is null) != string.IsNullOrWhiteSpace(unit) || quantity <= 0) return Validation("quantity", "Quantity và Unit phải cùng để trống hoặc cùng hợp lệ.");
        return null;
    }
    // Chức năng: giải mã RowVersion dùng cho kiểm soát đồng thời.
    // Input: text. Output: true kèm byte[] hoặc IActionResult lỗi.
    private bool TryVersion(string text, out byte[]? value, out IActionResult? error)
    {
        try { value = Convert.FromBase64String(text); if (value.Length == 16) { error = null; return true; } } catch (FormatException) { }
        value = null; error = UnprocessableEntity(Validation("rowVersion", "RowVersion không hợp lệ.")); return false;
    }
    // Chức năng: lưu thay đổi thành phần và xử lý xung đột.
    // Input: id, hàm tạo response, cancellationToken và noContent. Output: response thành công hoặc 409.
    private async Task<IActionResult> SaveComponent(Guid id, Func<object?> response, CancellationToken ct, bool noContent = false)
    {
        try { await db.SaveChangesAsync(ct); return noContent ? NoContent() : Ok(response()); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { errorCode = "CONCURRENCY_CONFLICT", message = "Dữ liệu đã được thay đổi bởi người khác.", id }); }
    }
    // Chức năng: tạo payload lỗi validation theo trường.
    // Input: field và message. Output: object lỗi.
    private static object Validation(string field, string message) => new { errorCode = "VALIDATION_ERROR", message = "Dữ liệu không hợp lệ.", errors = new Dictionary<string, string[]> { [field] = [message] } };
    // Chức năng: tạo response nguyên liệu có RowVersion.
    // Input: RecipeIngredient. Output: object phản hồi.
    private static object IngredientResponse(RecipeIngredient x) => new { x.Id, x.Name, x.Quantity, x.Unit, x.Notes, x.OrderIndex, rowVersion = Convert.ToBase64String(x.RowVersion) };
    // Chức năng: tạo response bước làm có RowVersion.
    // Input: RecipeStep. Output: object phản hồi.
    private static object StepResponse(RecipeStep x) => new { x.Id, x.StepNumber, x.Title, x.Description, x.TimerMinutes, x.ImageUrl, rowVersion = Convert.ToBase64String(x.RowVersion) };
}

public record IngredientRequest(string Name, decimal? Quantity, string? Unit, string? Notes);
public sealed record IngredientUpdateRequest(string Name, decimal? Quantity, string? Unit, string? Notes, int OrderIndex, string RowVersion) : IngredientRequest(Name, Quantity, Unit, Notes);
public record StepRequest(string? Title, string Description, int? TimerMinutes, string? ImageUrl);
public sealed record StepUpdateRequest(string? Title, string Description, int? TimerMinutes, string? ImageUrl, string RowVersion) : StepRequest(Title, Description, TimerMinutes, ImageUrl);
public sealed record VersionRequest(string RowVersion);
