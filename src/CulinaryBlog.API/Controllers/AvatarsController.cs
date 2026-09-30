using CulinaryBlog.Application.Files;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users/me/avatar")]
public sealed class AvatarsController(UserManager<ApplicationUser> users, IFileStorageService storage,
    IFileDeletionQueue deletions, ILogger<AvatarsController> logger) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageFile.MaxBytes + 32768)]
    public async Task<IActionResult> Upload([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (request.File.Length is < 1 or > ImageFile.MaxBytes)
            return UnprocessableEntity(new { errorCode = "INVALID_IMAGE_SIZE", message = "Ảnh phải có dung lượng từ 1 byte đến 5 MB." });
        await using var stream = request.File.OpenReadStream();
        var stored = await storage.UploadAsync(stream, request.File.ContentType, Path.GetExtension(request.File.FileName),
            "avatars/" + user.Id, ct);
        try
        {
            if (user.AvatarUrl is { } oldUrl) deletions.Enqueue(oldUrl);
            user.AvatarUrl = stored.Url;
            var result = await users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                deletions.Enqueue(stored.Url);
                return Conflict(new { errorCode = "AVATAR_UPDATE_CONFLICT", errors = result.Errors });
            }
            return StatusCode(201, new { avatarUrl = stored.Url });
        }
        catch
        {
            try { deletions.Enqueue(stored.Url); }
            catch (Exception cleanupError) { logger.LogError(cleanupError, "Failed to schedule cleanup for {Url}", stored.Url); }
            throw;
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (user.AvatarUrl is null) return NoContent();
        deletions.Enqueue(user.AvatarUrl);
        user.AvatarUrl = null;
        var result = await users.UpdateAsync(user);
        return result.Succeeded ? NoContent() : Conflict(new { errorCode = "AVATAR_UPDATE_CONFLICT", errors = result.Errors });
    }
}
