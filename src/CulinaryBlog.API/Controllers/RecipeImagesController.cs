// Tệp này cung cấp API quản lý ảnh công thức.
// Chức năng: tải ảnh (Upload), đặt ảnh đại diện (SetPrimary), xóa ảnh (Delete) và nhận dạng tệp (DetectFormat).

namespace CulinaryBlog.API.Controllers;

using System.Security.Claims;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Roles = AppRoles.Author + "," + AppRoles.Admin)]
[Route("api/v1/recipes/{recipeId:guid}/images")]
// Class điều phối metadata/quyền của FR-RCP-008 và gọi cổng IFileStorageService do FR-FILE hiện thực.
// Input: repository, Unit of Work, cổng lưu tệp và logger. Output: phản hồi HTTP cho thao tác ảnh.
public sealed class RecipeImagesController(IRecipeCommandRepository repository, IRecipeUnitOfWork unitOfWork, IFileStorageService storage, IFileDeletionQueue deletions, ILogger<RecipeImagesController> logger) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize + 1024 * 32)]
    // Chức năng: kiểm tra và tải ảnh JPEG/PNG/WebP/AVIF tối đa 5 MB.
    // Input: recipeId, ImageUploadRequest và cancellationToken. Output: thông tin ảnh đã lưu hoặc lỗi.
    public async Task<IActionResult> Upload(Guid recipeId, [FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        if (request.File is null || request.File.Length == 0 || request.File.Length > MaxFileSize)
            return UnprocessableEntity(Error("INVALID_IMAGE_SIZE", "Ảnh phải có dung lượng từ 1 byte đến 5 MB."));
        StoredFile stored;
        await using (var stream = request.File.OpenReadStream())
            stored = await storage.UploadAsync(stream, request.File.ContentType, Path.GetExtension(request.File.FileName), $"recipes/{recipeId}", ct);
        try
        {
            var order = await repository.GetNextImageOrderAsync(recipeId, ct);
            var isPrimary = !await repository.HasPrimaryImageAsync(recipeId, ct);
            var image = new RecipeImage { RecipeId = recipeId, OriginalUrl = stored.Url, AltText = request.AltText?.Trim(), IsPrimary = isPrimary, OrderIndex = order };
            repository.AddImage(image); await unitOfWork.SaveChangesAsync(ct);
            return StatusCode(201, ImageResponse(image));
        }
        catch
        {
            try { deletions.Enqueue(stored.Url); } catch (Exception ex) { logger.LogError(ex, "Failed to queue cleanup for {Url}", stored.Url); }
            throw;
        }
    }

    [HttpPost("{imageId:guid}/primary")]
    // Chức năng: chọn một ảnh làm ảnh đại diện duy nhất.
    // Input: recipeId, imageId, VersionRequest và cancellationToken. Output: ảnh đại diện mới hoặc lỗi.
    public async Task<IActionResult> SetPrimary(Guid recipeId, Guid imageId, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        var images = await repository.GetImagesAsync(recipeId, ct);
        var selected = images.SingleOrDefault(x => x.Id == imageId); if (selected is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version)) return UnprocessableEntity(Error("INVALID_ROW_VERSION", "RowVersion không hợp lệ."));
        repository.SetOriginalVersion(selected, version!);
        foreach (var image in images) image.IsPrimary = image.Id == imageId;
        try { await unitOfWork.SaveChangesAsync(ct); return Ok(ImageResponse(selected)); }
        catch (DbUpdateConcurrencyException) { return Conflict(Error("IMAGE_CONCURRENCY_CONFLICT", "Ảnh đã được thay đổi bởi người khác.")); }
        catch (DbUpdateException) { return Conflict(Error("PRIMARY_IMAGE_CONFLICT", "Ảnh chính vừa được thay đổi bởi yêu cầu khác.")); }
    }

    [HttpDelete("{imageId:guid}")]
    // Chức năng: xóa mềm bản ghi ảnh và xóa tệp vật lý.
    // Input: recipeId, imageId, VersionRequest và cancellationToken. Output: 204 hoặc lỗi.
    public async Task<IActionResult> Delete(Guid recipeId, Guid imageId, VersionRequest request, CancellationToken ct)
    {
        var access = await EditableRecipe(recipeId, ct); if (access is not null) return access;
        var image = await repository.GetImageAsync(recipeId, imageId, ct); if (image is null) return NotFound();
        if (!TryVersion(request.RowVersion, out var version)) return UnprocessableEntity(Error("INVALID_ROW_VERSION", "RowVersion không hợp lệ."));
        repository.SetOriginalVersion(image, version!);
        try
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            if (image.IsPrimary)
            {
                image.IsPrimary = false;
                await unitOfWork.SaveChangesAsync(ct);
                var replacement = (await repository.GetImagesAsync(recipeId, ct)).FirstOrDefault(x => x.Id != imageId);
                if (replacement is not null) replacement.IsPrimary = true;
            }
            foreach (var url in new[] { image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl }.Where(x => !string.IsNullOrEmpty(x)).Distinct())
                deletions.Enqueue(url!);
            image.IsDeleted = true;
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { return Conflict(Error("IMAGE_CONCURRENCY_CONFLICT", "Ảnh đã được thay đổi bởi người khác.")); }
        return NoContent();
    }

    // Chức năng: kiểm tra quyền sửa ảnh của owner/Admin.
    // Input: id công thức và cancellationToken. Output: null nếu được phép hoặc IActionResult lỗi.
    private async Task<IActionResult?> EditableRecipe(Guid id, CancellationToken ct)
    {
        var recipe = await repository.GetRecipeAsync(id, cancellationToken: ct); if (recipe is null) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return recipe.AuthorId == userId || User.IsInRole(AppRoles.Admin) ? null : Forbid();
    }
    // Chức năng: giải mã RowVersion Base64.
    // Input: text. Output: true kèm byte[] hoặc false.
    private static bool TryVersion(string text, out byte[]? version)
    {
        try { version = Convert.FromBase64String(text); return version.Length == 16; } catch (FormatException) { version = null; return false; }
    }
    // Chức năng: tạo response ảnh có RowVersion.
    // Input: RecipeImage. Output: thông tin ảnh.
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
