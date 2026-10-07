using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Xml.Linq;
using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Hangfire;
using Hangfire.PostgreSql;
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class JobsDatabaseFactAttribute : FactAttribute
{
    public JobsDatabaseFactAttribute() { if (Environment.GetEnvironmentVariable("RUN_JOB_OBS_TESTS") != "1") Skip = "Run scripts/test-jobs-observability.ps1 against the dedicated Docker stack."; }
}
[CollectionDefinition("Jobs database", DisableParallelization = true)]
public sealed class JobsDatabaseCollection { }

[Collection("Jobs database")]
public sealed class JobsObservabilityDatabaseTests
{
    private static ApplicationDbContext Database()
    {
        var connection = Environment.GetEnvironmentVariable("JOB_OBS_DB") ?? throw new InvalidOperationException("Set JOB_OBS_DB.");
        var parsed = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("localhost" or "127.0.0.1") || parsed.Database != "culinary_jobs_tests")
            throw new InvalidOperationException("These tests require the dedicated local culinary_jobs_tests database.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
    }
    private static HttpClient Client() => new() { BaseAddress = new Uri("http://localhost:5059"), Timeout = TimeSpan.FromSeconds(15) };
    private static async Task<Guid> TaskRow(ApplicationDbContext db, string kind, string target)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO culinary."BackgroundTasks" ("Id", "Kind", "TargetId", "DispatchedAt")
            VALUES ({id}, {kind}, {target}, now())
            """);
        return id;
    }
    private static async Task<bool> Completed(ApplicationDbContext db, Guid id) =>
        await db.Database.SqlQueryRaw<BackgroundTaskRow>("""
            SELECT "Id", "Kind", "TargetId", "CompletedAt" FROM culinary."BackgroundTasks"
            """).Where(x => x.Id == id).Select(x => x.CompletedAt != null).SingleAsync();
    private static async Task<ApplicationUser> User(ApplicationDbContext db)
    {
        var id = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser { Id = id, UserName = "job-" + id, DisplayName = "<Test user>",
            Email = id + "@example.invalid", IsActive = true };
        db.Users.Add(user); await db.SaveChangesAsync(); return user;
    }

    [JobsDatabaseFact]
    public async Task OutboxRollsBackWithBusinessTransactionAndDeduplicates()
    {
        await using var db = Database();
        var target = Guid.NewGuid().ToString();
        var queue = new DatabaseBackgroundTaskQueue(db);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await queue.WelcomeAsync(target, default);
            await tx.RollbackAsync();
        }
        var rows = db.Database.SqlQueryRaw<BackgroundTaskRow>("""
            SELECT "Id", "Kind", "TargetId", "CompletedAt" FROM culinary."BackgroundTasks"
            """);
        Assert.False(await rows.AnyAsync(x => x.TargetId == target));
        await queue.WelcomeAsync(target, default);
        await queue.WelcomeAsync(target, default);
        Assert.Equal(1, await rows.CountAsync(x => x.TargetId == target));
    }

    [JobsDatabaseFact]
    public async Task WelcomeEmailCompletesOnceAndRecordsCompletionInPostgres()
    {
        await using var db = Database();
        var user = await User(db);
        var id = await TaskRow(db, "welcome", user.Id);
        var email = new RecordingEmail();
        var job = new WelcomeEmailJob(db, email, NullLogger<WelcomeEmailJob>.Instance);
        await job.RunAsync(id, default);
        await job.RunAsync(id, default);
        Assert.Equal(1, email.Calls);
        Assert.True(await Completed(db, id));
    }

    [JobsDatabaseFact]
    public async Task FailedWelcomeLeavesTaskIncompleteForRetry()
    {
        await using var db = Database();
        var user = await User(db);
        var id = await TaskRow(db, "welcome", user.Id);
        var job = new WelcomeEmailJob(db, new RecordingEmail { Fail = true }, NullLogger<WelcomeEmailJob>.Instance);
        await Assert.ThrowsAsync<IOException>(() => job.RunAsync(id, default));
        Assert.False(await Completed(db, id));
        Assert.True(await db.Users.AnyAsync(x => x.Id == user.Id));
    }

    [JobsDatabaseFact]
    public async Task ImageVariantsHaveRequiredDimensionsAndPersistInPostgres()
    {
        await using var db = Database();
        var image = await ImageRecord(db);
        var id = await TaskRow(db, "resize", image.Id.ToString());
        var storage = new RecordingStorage();
        var job = new ImageResizeJob(db, new ImageReader(), storage, storage, NullLogger<ImageResizeJob>.Instance);
        await job.RunAsync(id, default);
        Assert.True(await Completed(db, id));
        using var thumb = new MagickImage(storage.Files[image.ThumbnailUrl!]);
        using var medium = new MagickImage(storage.Files[image.MediumUrl!]);
        Assert.Equal(300u, thumb.Width); Assert.Equal(300u, thumb.Height);
        Assert.Equal(800u, medium.Width); Assert.Equal(600u, medium.Height);
        Assert.Equal("/uploads/recipes/test/original.png", image.OriginalUrl);
        await job.RunAsync(id, default);
        Assert.Equal(2, storage.Files.Count);
    }

    [JobsDatabaseFact]
    public async Task FailedVariantUploadRetainsOriginalAndQueuesPartialCleanup()
    {
        await using var db = Database();
        var image = await ImageRecord(db);
        var id = await TaskRow(db, "resize", image.Id.ToString());
        var storage = new RecordingStorage { FailSecond = true };
        var job = new ImageResizeJob(db, new ImageReader(), storage, storage, NullLogger<ImageResizeJob>.Instance);
        await Assert.ThrowsAsync<IOException>(() => job.RunAsync(id, default));
        db.ChangeTracker.Clear();
        var persisted = await db.RecipeImages.SingleAsync(x => x.Id == image.Id);
        Assert.Null(persisted.ThumbnailUrl); Assert.Null(persisted.MediumUrl);
        Assert.Equal("/uploads/recipes/test/original.png", persisted.OriginalUrl);
        Assert.Single(storage.Deletions);
        Assert.False(await Completed(db, id));
    }

    [JobsDatabaseFact]
    public async Task DeletedImageIsSkippedWithoutCreatingVariants()
    {
        await using var db = Database();
        var image = await ImageRecord(db);
        image.IsDeleted = true; await db.SaveChangesAsync();
        var id = await TaskRow(db, "resize", image.Id.ToString());
        var storage = new RecordingStorage();
        await new ImageResizeJob(db, new ImageReader(), storage, storage, NullLogger<ImageResizeJob>.Instance).RunAsync(id, default);
        Assert.Empty(storage.Files);
        Assert.True(await Completed(db, id));
    }

    [JobsDatabaseFact]
    public async Task CorruptOriginalDoesNotChangeImageMetadata()
    {
        await using var db = Database();
        var image = await ImageRecord(db);
        var id = await TaskRow(db, "resize", image.Id.ToString());
        var storage = new RecordingStorage();
        var job = new ImageResizeJob(db, new ImageReader { Corrupt = true }, storage, storage, NullLogger<ImageResizeJob>.Instance);
        await Assert.ThrowsAnyAsync<Exception>(() => job.RunAsync(id, default));
        Assert.Null(image.ThumbnailUrl); Assert.Null(image.MediumUrl);
        Assert.Empty(storage.Files);
        Assert.False(await Completed(db, id));
    }

    [JobsDatabaseFact]
    public async Task SitemapContainsPublishedRecipesAndCategoriesOnly()
    {
        await using var db = Database();
        var dir = Path.Combine(Path.GetTempPath(), "culinary-job-test-" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Site:BaseUrl"] = "https://culinary.test" }).Build();
        await new SitemapGenerationJob(db, new TestEnvironment(dir), config, NullLogger<SitemapGenerationJob>.Instance).RunAsync(default);
        var document = XDocument.Load(Path.Combine(dir, "wwwroot", "sitemap.xml"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = document.Root!.Elements(ns + "url").Select(x => x.Element(ns + "loc")!.Value).ToHashSet();
        var published = await db.Recipes.Where(x => x.Status == CulinaryBlog.Domain.Enums.RecipeStatus.Published).Select(x => x.Slug).ToListAsync();
        var drafts = await db.Recipes.Where(x => x.Status != CulinaryBlog.Domain.Enums.RecipeStatus.Published).Select(x => x.Slug).ToListAsync();
        Assert.All(published, slug => Assert.Contains("https://culinary.test/recipes/" + slug, urls));
        Assert.All(drafts, slug => Assert.DoesNotContain("https://culinary.test/recipes/" + slug, urls));
        var categories = await db.Categories.Select(x => x.Slug).ToListAsync();
        Assert.All(categories, slug => Assert.Contains("https://culinary.test/categories/" + slug, urls));
        Assert.Contains("https://culinary.test/", urls);
        Assert.Contains("https://culinary.test/search", urls);
        Assert.Empty(Directory.GetFiles(Path.Combine(dir, "wwwroot"), "*.tmp"));
    }

    [JobsDatabaseFact]
    public async Task RetryPoliciesMatchSpecification()
    {
        var welcome = typeof(WelcomeEmailJob).GetMethod("RunAsync")!.GetCustomAttribute<AutomaticRetryAttribute>()!;
        Assert.Equal(3, welcome.Attempts);
        Assert.Equal(new[] { 60, 300, 1800 }, welcome.DelaysInSeconds);
        Assert.Equal(AttemptsExceededAction.Fail, welcome.OnAttemptsExceeded);
        Assert.Equal(3, typeof(ImageResizeJob).GetMethod("RunAsync")!.GetCustomAttribute<AutomaticRetryAttribute>()!.Attempts);
        Assert.Equal(2, typeof(SitemapGenerationJob).GetMethod("RunAsync")!.GetCustomAttribute<AutomaticRetryAttribute>()!.Attempts);
        await Task.CompletedTask;
    }

    [JobsDatabaseFact]
    public async Task HealthProbesCheckRealDependenciesAndCorrelationHeaderIsValidated()
    {
        using var client = Client();
        foreach (var path in new[] { "/health", "/health/live", "/health/ready" })
        {
            var response = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "jobs-test-123");
        var result = await client.SendAsync(request);
        Assert.Equal("jobs-test-123", result.Headers.GetValues("X-Correlation-ID").Single());
        var invalid = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        invalid.Headers.Add("X-Correlation-ID", "invalid header value");
        var sanitized = await client.SendAsync(invalid);
        Assert.NotEqual("invalid header value", sanitized.Headers.GetValues("X-Correlation-ID").Single());
    }

    [JobsDatabaseFact]
    public async Task DatabaseHealthCheckDetectsUnreachableDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unreachable;Username=test;Password=test;Timeout=1").Options;
        await using var db = new ApplicationDbContext(options);
        var result = await new DatabaseHealthCheck(db).CheckHealthAsync(new());
        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, result.Status);
    }

    [JobsDatabaseFact]
    public async Task DashboardRejectsAnonymousAndAuthorButAcceptsAdmin()
    {
        using var client = Client();
        var anonymous = await client.GetAsync("/hangfire");
        Assert.True(anonymous.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
        string Token(string role)
        {
            var jwt = new JwtSecurityToken("CulinaryBlog", "CulinaryBlogApp",
                [new Claim(ClaimTypes.NameIdentifier, "jobs-dashboard-test"), new Claim(ClaimTypes.Role, role)],
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    "CulinaryJobsDockerDevelopmentSecretKeyAtLeast32Characters")), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Author"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/hangfire")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Admin"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/hangfire")).StatusCode);
        var sitemap = await client.PostAsync("/api/v1/admin/jobs/sitemap", null);
        Assert.Equal(HttpStatusCode.Accepted, sitemap.StatusCode);
        HttpResponseMessage? served = null;
        for (var i = 0; i < 60; i++)
        {
            served = await client.GetAsync("/sitemap.xml");
            if (served.StatusCode == HttpStatusCode.OK) break;
            served.Dispose();
            await Task.Delay(1000);
        }
        Assert.Equal(HttpStatusCode.OK, served!.StatusCode);
        var document = XDocument.Parse(await served.Content.ReadAsStringAsync());
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        Assert.NotEmpty(document.Root!.Elements(ns + "url"));
        served.Dispose();

    }

    [JobsDatabaseFact]
    public async Task RegistrationQueuesRealWelcomeEmailAndMailpitReceivesHtml()
    {
        using var client = Client();
        var name = "welcome" + Guid.NewGuid().ToString("N")[..10];
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new {
            email = name + "@example.invalid", userName = name, displayName = "<Welcome Test>", password = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<CulinaryBlog.Application.DTOs.Auth.AuthResponse>())!;
        await using var db = Database();
        var completed = false;
        for (var i = 0; i < 60; i++)
        {
            completed = await db.Database.SqlQueryRaw<BackgroundTaskRow>("""
                SELECT "Id", "Kind", "TargetId", "CompletedAt" FROM culinary."BackgroundTasks"
                """).AnyAsync(x => x.Kind == "welcome" && x.TargetId == auth.User.Id && x.CompletedAt != null);
            if (completed) break;
            await Task.Delay(1000);
        }
        Assert.True(completed, "Welcome task did not finish.");
        using var mail = new HttpClient();
        var messages = await mail.GetStringAsync("http://localhost:18025/api/v1/search?query=" + Uri.EscapeDataString(name + "@example.invalid"));
        using var list = System.Text.Json.JsonDocument.Parse(messages);
        var message = list.RootElement.GetProperty("messages").EnumerateArray().First();
        var id = message.GetProperty("ID").GetString();
        var detail = await mail.GetStringAsync("http://localhost:18025/api/v1/message/" + id);
        using var parsed = System.Text.Json.JsonDocument.Parse(detail);
        var html = parsed.RootElement.GetProperty("HTML").GetString()!;
        Assert.Contains("&lt;Welcome Test&gt;", html);
        Assert.Contains("http://localhost:3000", html);
    }

    [JobsDatabaseFact]
    public async Task RealUploadRunsInHangfireAndStoresReadableVariantsOnMinio()
    {
        using var client = Client();
        var name = "image" + Guid.NewGuid().ToString("N")[..10];
        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new {
            email = name + "@example.invalid", userName = name, displayName = name, password = "Password123!"
        });
        registered.EnsureSuccessStatusCode();
        var auth = (await registered.Content.ReadFromJsonAsync<CulinaryBlog.Application.DTOs.Auth.AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await using var db = Database();
        var category = await db.Categories.Select(x => x.Id).FirstAsync();
        var created = await client.PostAsJsonAsync("/api/v1/recipes", new {
            title = "Recipe resize integration " + name, description = "Image processing integration fixture.",
            prepTime = 10, cookTime = 20, servings = 2, difficulty = 1, categoryId = category
        });
        created.EnsureSuccessStatusCode();
        using var recipe = System.Text.Json.JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var recipeId = recipe.RootElement.GetProperty("id").GetGuid();
        foreach (var format in new[] { MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.WebP, MagickFormat.Avif })
        {
            using var original = new MagickImage(MagickColors.Blue, 950, 650) { Format = format };
            var extension = format.ToString().ToLowerInvariant();
            var mime = format == MagickFormat.Jpeg ? "image/jpeg" : "image/" + extension;
            using var form = new MultipartFormDataContent();
            var part = new ByteArrayContent(original.ToByteArray());
            part.Headers.ContentType = new MediaTypeHeaderValue(mime);
            form.Add(part, "file", "test." + extension);
            var upload = await client.PostAsync("/api/v1/recipes/" + recipeId + "/images", form);
            upload.EnsureSuccessStatusCode();
            using var metadata = System.Text.Json.JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
            var imageId = metadata.RootElement.GetProperty("id").GetGuid();
            RecipeImage? image = null;
            for (var i = 0; i < 60; i++)
            {
                image = await db.RecipeImages.AsNoTracking().SingleAsync(x => x.Id == imageId);
                if (image.ThumbnailUrl is not null && image.MediumUrl is not null) break;
                await Task.Delay(1000);
            }
            Assert.NotNull(image!.ThumbnailUrl); Assert.NotNull(image.MediumUrl);
            using var publicClient = new HttpClient();
            Assert.Equal(HttpStatusCode.OK, (await publicClient.GetAsync(image.OriginalUrl)).StatusCode);
            using var thumb = new MagickImage(await publicClient.GetByteArrayAsync(image.ThumbnailUrl));
            using var medium = new MagickImage(await publicClient.GetByteArrayAsync(image.MediumUrl));
            Assert.Equal(300u, thumb.Width); Assert.Equal(300u, thumb.Height);
            Assert.Equal(800u, medium.Width); Assert.Equal(600u, medium.Height);
        }
        var ingredient = await client.PostAsJsonAsync("/api/v1/recipes/" + recipeId + "/ingredients",
            new { name = "Test ingredient", quantity = 1, unit = "g" });
        ingredient.EnsureSuccessStatusCode();
        var step = await client.PostAsJsonAsync("/api/v1/recipes/" + recipeId + "/steps",
            new { title = "Test step", description = "Prepare test ingredients.", timerMinutes = 1 });
        step.EnsureSuccessStatusCode();
        var publish = await client.PostAsJsonAsync("/api/v1/recipes/" + recipeId + "/publish",
            new { rowVersion = recipe.RootElement.GetProperty("rowVersion").GetString() });
        publish.EnsureSuccessStatusCode();
    }

    [JobsDatabaseFact]
    public async Task RecurringSitemapUsesDailyUtcSchedulePersistedInPostgres()
    {
        await using var db = Database();
        var cron = await db.Database.SqlQueryRaw<string>("""
            SELECT value AS "Value" FROM hangfire.hash WHERE key = 'recurring-job:culinary-sitemap' AND field = 'Cron'
            """).SingleAsync();
        Assert.Equal("0 2 * * *", cron);
        var timezone = await db.Database.SqlQueryRaw<string>("""
            SELECT value AS "Value" FROM hangfire.hash WHERE key = 'recurring-job:culinary-sitemap' AND field = 'TimeZoneId'
            """).SingleAsync();
        Assert.Equal("UTC", timezone);
    }

    [JobsDatabaseFact]
    public async Task HangfirePersistsRetryAndFailedStateAfterRetryBudgetIsExhausted()
    {
        await using var db = Database();
        var image = await ImageRecord(db);
        // The deliberately unowned original URL causes a real worker error.
        var taskId = await TaskRow(db, "resize", image.Id.ToString());
        Hangfire.GlobalConfiguration.Configuration.UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(Environment.GetEnvironmentVariable("JOB_OBS_DB")!));
        var client = new BackgroundJobClient(JobStorage.Current);
        var jobId = client.Enqueue<ImageResizeJob>(job => job.RunAsync(taskId, CancellationToken.None));
        using var connection = JobStorage.Current.GetConnection();
        async Task WaitState(string state)
        {
            for (var i = 0; i < 45; i++)
            {
                if (connection.GetStateData(jobId)?.Name == state) return;
                await Task.Delay(1000);
            }
            Assert.Fail("Job did not enter " + state + "; actual: " + connection.GetStateData(jobId)?.Name);
        }
        await WaitState("Scheduled");
        // Advance the persisted retry budget rather than waiting minutes in CI.
        connection.SetJobParameter(jobId, "RetryCount", "3");
        Assert.True(client.ChangeState(jobId, new Hangfire.States.EnqueuedState(), "Scheduled"));
        await WaitState("Failed");
        Assert.False(await Completed(db, taskId));
        var persisted = await db.RecipeImages.AsNoTracking().SingleAsync(x => x.Id == image.Id);
        Assert.Equal(image.OriginalUrl, persisted.OriginalUrl);
        Assert.Null(persisted.ThumbnailUrl);
    }

    private static async Task<RecipeImage> ImageRecord(ApplicationDbContext db)
    {
        var recipeId = await db.Recipes.Select(x => x.Id).FirstAsync();
        var image = new RecipeImage { RecipeId = recipeId, OriginalUrl = "/uploads/recipes/test/original.png" };
        db.RecipeImages.Add(image); await db.SaveChangesAsync(); return image;
    }
    private sealed class RecordingEmail : IEmailService
    {
        public int Calls; public bool Fail;
        public Task SendWelcomeEmailAsync(string email, string name, CancellationToken ct = default)
        { Calls++; if (Fail) throw new IOException("SMTP unavailable"); return Task.CompletedTask; }
    }
    private sealed class ImageReader : IOriginalImageReader
    {
        public bool Corrupt;
        public Task<MemoryStream> ReadAsync(string url, CancellationToken ct)
        {
            var output = new MemoryStream();
            if (Corrupt) output.Write([0x89, 0x50, 0x4e, 0x47]);
            else { using var image = new MagickImage(MagickColors.Green, 1000, 700); image.Format = MagickFormat.Png; image.Write(output); }
            output.Position = 0; return Task.FromResult(output);
        }
    }
    private sealed class RecordingStorage : IFileStorageService, IFileDeletionQueue
    {
        public Dictionary<string, byte[]> Files = new();
        public List<string> Deletions = new();
        public bool FailSecond;
        public async Task<StoredFile> UploadAsync(Stream stream, string mime, string ext, string folder, CancellationToken ct)
        {
            if (FailSecond && Files.Count == 1) throw new IOException("Storage unavailable");
            using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes, ct);
            var url = "/uploads/" + folder + "/" + Guid.NewGuid().ToString("N") + ext;
            Files.Add(url, bytes.ToArray()); return new(url);
        }
        public Task DeleteAsync(string url, CancellationToken ct) { Files.Remove(url); return Task.CompletedTask; }
        public void Enqueue(string url) => Deletions.Add(url);
    }
    private sealed class TestEnvironment(string directory) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "JobsTests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = directory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(directory, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
