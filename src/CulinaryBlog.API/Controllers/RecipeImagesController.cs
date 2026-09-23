// Tệp này cung cấp API quản lý ảnh công thức.
// Chức năng: tải ảnh (Upload), đặt ảnh đại diện (SetPrimary), xóa ảnh (Delete) và nhận dạng tệp (DetectFormat).

namespace CulinaryBlog.API.Controllers;

using System.Security.Claims;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
[Route("api/v1/recipes/{recipeId:guid}/images")]
// Class điều phối FR-RCP-008 và lưu tệp qua IFileStorageService.
// Input: DbContext, dịch vụ lưu tệp và logger. Output: phản hồi HTTP cho thao tác ảnh.
public sealed class RecipeImagesController(ApplicationDbContext db, IFileStorageService storage, ILogger<RecipeImagesController> logger) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize + 1024 * 32)]
    // Chức năng: kiểm tra và tải ảnh JPEG/PNG/WebP tối đa 5 MB.
    // Input: recipeId, ImageUploadRequest và cancellationToken. Output: thông tin ảnh đã lưu hoặc lỗi.
    public async Task<IActionResult> Upload(Guid recipeId, [FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        if (request.File is null || request.File.Length == 0 || request.File.Length > MaxFileSize)
            return UnprocessableEntity(Error("INVALID_IMAGE_SIZE", "Ảnh phải có dung lượng từ 1 byte đến 5 MB."));
        var format = await DetectFormat(request.File, ct);
        if (format is null) return UnprocessableEntity(Error("INVALID_IMAGE_TYPE", "Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP hợp lệ."));

        StoredFile stored;
        await using (var stream = request.File.OpenReadStream())
            stored = await storage.UploadAsync(stream, format.Value.ContentType, format.Value.Extension, $"recipes/{recipeId}", ct);
        try
        {
            var order = await db.RecipeImages.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.OrderIndex).MaxAsync(ct) ?? -1;
            var isPrimary = !await db.RecipeImages.AnyAsync(x => x.RecipeId == recipeId && x.IsPrimary, ct);
            var image = new RecipeImage { RecipeId = recipeId, OriginalUrl = stored.Url, AltText = request.AltText?.Trim(), IsPrimary = isPrimary, OrderIndex = order + 1 };
            db.RecipeImages.Add(image); await db.SaveChangesAsync(ct);
            return StatusCode(201, ImageResponse(image));
        }
        catch
        {
            await storage.DeleteAsync(stored.Url, ct);
            throw;
        }
    }

    [HttpPost("{imageId:guid}/primary")]
    // Chức năng: chọn một ảnh làm ảnh đại diện duy nhất.
    // Input: recipeId, imageId, VersionRequest và cancellationToken. Output: ảnh đại diện mới hoặc lỗi.
    public async Task<IActionResult> SetPrimary(Guid recipeId, Guid imageId, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        var images = await db.RecipeImages.Where(x => x.RecipeId == recipeId).ToListAsync(ct);
        var selected = images.SingleOrDefault(x => x.Id == imageId); if (selected is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version)) return UnprocessableEntity(Error("INVALID_ROW_VERSION", "RowVersion không hợp lệ."));
        db.Entry(selected).Property(x => x.RowVersion).OriginalValue = version!;
        foreach (var image in images) image.IsPrimary = image.Id == imageId;
        try { await db.SaveChangesAsync(ct); return Ok(ImageResponse(selected)); }
        catch (DbUpdateConcurrencyException) { return Conflict(Error("IMAGE_CONCURRENCY_CONFLICT", "Ảnh đã được thay đổi bởi người khác.")); }
        catch (DbUpdateException) { return Conflict(Error("PRIMARY_IMAGE_CONFLICT", "Ảnh chính vừa được thay đổi bởi yêu cầu khác.")); }
    }

    [HttpDelete("{imageId:guid}")]
    // Chức năng: xóa mềm bản ghi ảnh và xóa tệp vật lý.
    // Input: recipeId, imageId, VersionRequest và cancellationToken. Output: 204 hoặc lỗi.
    public async Task<IActionResult> Delete(Guid recipeId, Guid imageId, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        var image = await db.RecipeImages.SingleOrDefaultAsync(x => x.Id == imageId && x.RecipeId == recipeId, ct); if (image is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version)) return UnprocessableEntity(Error("INVALID_ROW_VERSION", "RowVersion không hợp lệ."));
        db.Entry(image).Property(x => x.RowVersion).OriginalValue = version!;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (image.IsPrimary)
            {
                image.IsPrimary = false;
                await db.SaveChangesAsync(ct);
                var replacement = await db.RecipeImages.Where(x => x.RecipeId == recipeId && x.Id != imageId).OrderBy(x => x.OrderIndex).FirstOrDefaultAsync(ct);
                if (replacement is not null) replacement.IsPrimary = true;
            }
            image.IsDeleted = true;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { return Conflict(Error("IMAGE_CONCURRENCY_CONFLICT", "Ảnh đã được thay đổi bởi người khác.")); }
        try { await storage.DeleteAsync(image.OriginalUrl, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete stored image {ImageUrl}", image.OriginalUrl); }
        return NoContent();
    }

    // Chức năng: kiểm tra quyền sửa ảnh của owner/Admin.
    // Input: id công thức và cancellationToken. Output: null nếu được phép hoặc IActionResult lỗi.
    private async Task<IActionResult?> EditableRecipe(Guid id, CancellationToken ct)
    {
        var recipe = await db.Recipes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (recipe is null) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return recipe.AuthorId == userId || User.IsInRole(AppRoles.Admin) ? null : Forbid();
    }
    // Chức năng: giải mã RowVersion Base64.
    // Input: text. Output: true kèm byte[] hoặc false.
    private static bool TryVersion(string text, out byte[]? version)
    {
        try { version = Convert.FromBase64String(text); return version.Length == 16; } catch (FormatException) { version = null; return false; }
    }
    // Chức năng: đọc chữ ký tệp để nhận dạng JPEG, PNG hoặc WebP.
    // Input: IFormFile và cancellationToken. Output: content type, extension hoặc null.
    private static async Task<(string ContentType, string Extension)?> DetectFormat(IFormFile file, CancellationToken ct)
    {
        var bytes = new byte[12]; await using var stream = file.OpenReadStream(); var read = await stream.ReadAsync(bytes, ct);
        if (read >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ("image/jpeg", ".jpg");
        if (read >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ("image/png", ".png");
        if (read >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return ("image/webp", ".webp");
        return null;
    }
    // Chức năng: tạo response ảnh có RowVersion.
    // Input: RecipeImage. Output: object phản hồi.
    private static object ImageResponse(RecipeImage x) => new { x.Id, x.OriginalUrl, x.MediumUrl, x.ThumbnailUrl, x.AltText, x.IsPrimary, x.OrderIndex, rowVersion = Convert.ToBase64String(x.RowVersion) };
    // Chức năng: tạo payload lỗi ảnh thống nhất.
    // Input: code và message. Output: object lỗi.
    private static object Error(string code, string message) => new { errorCode = code, message };
}

// Class nhận dữ liệu multipart khi upload ảnh.
// Input: File và AltText. Output: mô hình request được model binder tạo.
public sealed class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    public string? AltText { get; init; }
}
