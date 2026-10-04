using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.API.Services;

// Schedule before committing metadata changes: a failed enqueue leaves the metadata intact.
// The reference check also protects files if the following database commit fails.
public sealed class HangfireFileDeletionQueue(IBackgroundJobClient jobs) : IFileDeletionQueue
{
    public void Enqueue(string url)
    {
        try
        {
        jobs.Schedule<FileDeletionJob>(job => job.DeleteAsync(url, CancellationToken.None), TimeSpan.FromMinutes(1));
        }
        catch (Exception ex) { throw new CulinaryBlog.Application.Exceptions.FileStorageException("Không thể lên lịch xóa tệp.", ex); }
    }
}

public sealed class FileDeletionJob(ApplicationDbContext db, IFileStorageService storage)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task DeleteAsync(string url, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.AvatarUrl == url, ct)
            || await db.RecipeImages.AnyAsync(x => x.OriginalUrl == url || x.MediumUrl == url || x.ThumbnailUrl == url, ct)
            || await db.RecipeSteps.AnyAsync(x => x.ImageUrl == url, ct))
            throw new InvalidOperationException("File is still referenced; deletion postponed.");
        await storage.DeleteAsync(url, ct);
    }
}
