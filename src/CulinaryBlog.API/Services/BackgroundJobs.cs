using System.Xml.Linq;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Storage;
using Hangfire;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.API.Services;

public sealed class WelcomeEmailJob(ApplicationDbContext db, IEmailService email, ILogger<WelcomeEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task RunAsync(Guid taskId, CancellationToken ct)
    {
        using var activity = ObservabilityTelemetry.Activities.StartActivity("job.welcome");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var task = await BackgroundTaskExecution.LockAsync(db, taskId, "welcome", ct);
        if (task is null) return;
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == task.TargetId, ct);
        try
        {
            if (user?.Email is not null) await email.SendWelcomeEmailAsync(user.Email, user.DisplayName, ct);
            await BackgroundTaskExecution.CompleteAsync(db, taskId, ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Welcome email task {TaskId} completed", taskId);
        }
        catch (Exception ex) { logger.LogError(ex, "Welcome email task {TaskId} failed", taskId); throw; }
    }
}

public interface IOriginalImageReader
{
    Task<MemoryStream> ReadAsync(string url, CancellationToken ct);
}
public sealed class OriginalImageReader(IServiceProvider services, IWebHostEnvironment environment, IConfiguration configuration) : IOriginalImageReader
{
    public async Task<MemoryStream> ReadAsync(string url, CancellationToken ct)
    {
        var output = new MemoryStream();
        try
        {
            if ((configuration["FileStorage:Provider"] ?? "Minio").Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(url, @"^/uploads/recipes/[A-Za-z0-9_-]+/[a-fA-F0-9]{32}\.(jpg|jpeg|png|webp|avif)$"))
                    throw new ArgumentException("Image URL is not owned by local storage.");
                await using var input = File.OpenRead(Path.Combine(environment.ContentRootPath, "wwwroot", url.TrimStart('/')));
                if (input.Length > 5 * 1024 * 1024) throw new InvalidOperationException("Original image is too large.");
                await input.CopyToAsync(output, ct);
            }
            else
            {
                var client = services.GetRequiredService<IMinioClient>();
                var settings = services.GetRequiredService<IOptions<MinioStorageOptions>>();
                var key = new MinioFileStorageService(client, settings).GetObjectName(url);
                var stat = await client.StatObjectAsync(new StatObjectArgs().WithBucket(settings.Value.BucketName).WithObject(key), ct);
                if (stat.Size > 5 * 1024 * 1024) throw new InvalidOperationException("Original image is too large.");
                await client.GetObjectAsync(new GetObjectArgs().WithBucket(settings.Value.BucketName).WithObject(key)
                    .WithCallbackStream(stream => stream.CopyTo(output)), ct);
            }
            output.Position = 0;
            return output;
        }
        catch { output.Dispose(); throw; }
    }
}
public sealed class ImageResizeJob(ApplicationDbContext db, IOriginalImageReader originals, IFileStorageService storage,
    IFileDeletionQueue deletions, ILogger<ImageResizeJob> logger)
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task RunAsync(Guid taskId, CancellationToken ct)
    {
        using var activity = ObservabilityTelemetry.Activities.StartActivity("job.resize");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var task = await BackgroundTaskExecution.LockAsync(db, taskId, "resize", ct);
        if (task is null) return;
        var image = await db.RecipeImages.SingleOrDefaultAsync(x => x.Id == Guid.Parse(task.TargetId), ct);
        var uploaded = new List<string>();
        try
        {
            if (image is not null && (image.ThumbnailUrl is null || image.MediumUrl is null))
            {
                await using var input = await originals.ReadAsync(image.OriginalUrl, ct);
                var info = new MagickImageInfo(input);
                if ((ulong)info.Width * info.Height > 40_000_000) throw new InvalidOperationException("Image dimensions exceed the processing limit.");
                input.Position = 0;
                using var source = new MagickImage(input);
                if ((ulong)source.Width * source.Height > 40_000_000) throw new InvalidOperationException("Image dimensions exceed the processing limit.");
                source.AutoOrient();
                source.Strip();
                async Task<string> Variant(uint width, uint height)
                {
                    using var resized = source.Clone();
                    resized.Resize(new MagickGeometry(width, height) { FillArea = true });
                    resized.Extent(width, height, Gravity.Center);
                    resized.Format = MagickFormat.Jpeg;
                    await using var output = new MemoryStream();
                    resized.Write(output);
                    output.Position = 0;
                    var saved = await storage.UploadAsync(output, "image/jpeg", ".jpg", $"recipes/{image.RecipeId}", ct);
                    uploaded.Add(saved.Url);
                    return saved.Url;
                }
                var oldUrls = new[] { image.ThumbnailUrl, image.MediumUrl }.Where(x => x is not null).ToArray();
                image.ThumbnailUrl = await Variant(300, 300);
                image.MediumUrl = await Variant(800, 600);
                await db.SaveChangesAsync(ct);
                foreach (var old in oldUrls) deletions.Enqueue(old!);
            }
            await BackgroundTaskExecution.CompleteAsync(db, taskId, ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Image resize task {TaskId} completed", taskId);
        }
        catch (Exception ex)
        {
            // Failed processing keeps OriginalUrl intact. Clean up variants created
            // before a failed upload or optimistic concurrency conflict.
            foreach (var url in uploaded)
                try { deletions.Enqueue(url); } catch (Exception cleanup) { logger.LogError(cleanup, "Variant cleanup could not be queued"); }
            logger.LogError(ex, "Image resize task {TaskId} failed; original image retained", taskId);
            throw;
        }
    }
}
public sealed class SitemapGenerationJob(ApplicationDbContext db, IWebHostEnvironment environment, IConfiguration configuration,
    ILogger<SitemapGenerationJob> logger)
{
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task RunAsync(CancellationToken ct)
    {
        using var activity = ObservabilityTelemetry.Activities.StartActivity("job.sitemap");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2312756002)", ct);
        var baseUrl = configuration["Site:BaseUrl"] ?? "http://localhost:3000";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var site) || site.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(site.UserInfo) || !string.IsNullOrEmpty(site.Query) || !string.IsNullOrEmpty(site.Fragment))
            throw new InvalidOperationException("Site:BaseUrl must be an HTTP(S) URL without credentials, query or fragment.");
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var root = new XElement(ns + "urlset");
        void Add(string path, DateTimeOffset? lastmod = null)
        {
            var url = new XElement(ns + "url", new XElement(ns + "loc", baseUrl.TrimEnd('/') + path));
            if (lastmod.HasValue) url.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-ddTHH:mm:sszzz")));
            root.Add(url);
        }
        Add("/"); Add("/search");
        var recipes = await db.Recipes.AsNoTracking().Where(x => x.Status == RecipeStatus.Published)
            .OrderBy(x => x.Id).Select(x => new { x.Slug, Lastmod = x.UpdatedAt ?? x.CreatedAt }).ToListAsync(ct);
        foreach (var recipe in recipes) Add("/recipes/" + Uri.EscapeDataString(recipe.Slug), recipe.Lastmod);
        var categories = await db.Categories.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Slug, Lastmod = x.UpdatedAt ?? x.CreatedAt }).ToListAsync(ct);
        foreach (var category in categories) Add("/categories/" + Uri.EscapeDataString(category.Slug), category.Lastmod);
        var directory = Path.Combine(environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, "sitemap-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var file = File.Create(temp))
                await new XDocument(new XDeclaration("1.0", "utf-8", null), root).SaveAsync(file, SaveOptions.None, ct);
            File.Move(temp, Path.Combine(directory, "sitemap.xml"), true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        await tx.CommitAsync(ct);
        logger.LogInformation("Sitemap generated with {UrlCount} URLs", root.Elements().Count());
    }
}
