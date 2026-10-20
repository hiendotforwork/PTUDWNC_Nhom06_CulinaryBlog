using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.API.Services;

public sealed class BackgroundTaskRow
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public string TargetId { get; set; } = "";
    public DateTimeOffset? CompletedAt { get; set; }
}
public sealed class DatabaseBackgroundTaskQueue(ApplicationDbContext db) : IBackgroundTaskQueue
{
    public Task WelcomeAsync(string userId, CancellationToken ct) => AddAsync("welcome", userId, ct);
    public Task ResizeAsync(Guid imageId, CancellationToken ct) => AddAsync("resize", imageId.ToString(), ct);
    public Task EmailConfirmationAsync(string userId, CancellationToken ct) => AddAsync("email-verification", userId, ct);
    private Task AddAsync(string kind, string target, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            if (kind is "welcome" or "resize")
            {
                var existing = BackgroundTaskExecution.InMemoryTasks.Values.Any(t => t.Kind == kind && t.TargetId == target);
                if (existing) return Task.CompletedTask;
            }
            var taskId = Guid.NewGuid();
            BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
            {
                Id = taskId,
                Kind = kind,
                TargetId = target
            };
            return Task.CompletedTask;
        }

        return db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO culinary."BackgroundTasks" ("Id", "Kind", "TargetId")
            VALUES ({Guid.NewGuid()}, {kind}, {target})
            ON CONFLICT DO NOTHING
            """, ct);
    }
}
public sealed class NoBackgroundTaskQueue : IBackgroundTaskQueue
{
    public Task WelcomeAsync(string userId, CancellationToken ct) => Task.CompletedTask;
    public Task ResizeAsync(Guid imageId, CancellationToken ct) => Task.CompletedTask;
    public Task EmailConfirmationAsync(string userId, CancellationToken ct) => Task.CompletedTask;
}

// Outbox and business records commit together. Dispatch may repeat after a crash;
// job execution locks the task and checks CompletedAt before applying changes.
public sealed class BackgroundTaskDispatcher(IServiceScopeFactory scopes, ILogger<BackgroundTaskDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await DispatchAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Background task dispatch failed; pending tasks remain in PostgreSQL"); }
            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    public static async Task DispatchAsync(ApplicationDbContext db, IBackgroundJobClient jobs, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2312756001)", ct);
        var tasks = await db.Database.SqlQueryRaw<BackgroundTaskRow>("""
            SELECT "Id", "Kind", "TargetId", "CompletedAt" FROM culinary."BackgroundTasks"
            WHERE "DispatchedAt" IS NULL ORDER BY "CreatedAt" LIMIT 50
            """).ToListAsync(ct);
        foreach (var task in tasks)
        {
            if (task.Kind == "welcome")
                jobs.Enqueue<WelcomeEmailJob>(job => job.RunAsync(task.Id, CancellationToken.None));
            else if (task.Kind == "email-verification")
                jobs.Enqueue<EmailVerificationJob>(job => job.RunAsync(task.Id, CancellationToken.None));
            else
                jobs.Enqueue<ImageResizeJob>(job => job.RunAsync(task.Id, CancellationToken.None));
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE culinary."BackgroundTasks" SET "DispatchedAt" = now() WHERE "Id" = {task.Id}
                """, ct);
        }
        await tx.CommitAsync(ct);
    }
}

public static class BackgroundTaskExecution
{
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, BackgroundTaskRow> InMemoryTasks = new();

    public static async Task<BackgroundTaskRow?> LockAsync(ApplicationDbContext db, Guid id, string kind, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            if (InMemoryTasks.TryGetValue(id, out var inMemRow))
            {
                if (inMemRow.Kind != kind) throw new InvalidOperationException("Unexpected task kind.");
                return inMemRow.CompletedAt is null ? inMemRow : null;
            }
            return null;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({id.ToString()}))", ct);
        var row = await db.Database.SqlQueryRaw<BackgroundTaskRow>("""
            SELECT "Id", "Kind", "TargetId", "CompletedAt" FROM culinary."BackgroundTasks"
            """).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is not null && row.Kind != kind) throw new InvalidOperationException("Unexpected task kind.");
        return row?.CompletedAt is null ? row : null;
    }
    public static Task CompleteAsync(ApplicationDbContext db, Guid id, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            if (InMemoryTasks.TryGetValue(id, out var inMemRow))
            {
                inMemRow.CompletedAt = DateTimeOffset.UtcNow;
            }
            return Task.CompletedTask;
        }

        return db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE culinary."BackgroundTasks" SET "CompletedAt" = now() WHERE "Id" = {id}
            """, ct);
    }
}
