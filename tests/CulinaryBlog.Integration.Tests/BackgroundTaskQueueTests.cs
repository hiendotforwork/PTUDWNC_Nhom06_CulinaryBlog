namespace CulinaryBlog.Integration.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.API.Services;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class BackgroundTaskQueueTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var dbName = "BackgroundTaskQueueTests_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task TwoVerificationRequestsForSameUser_CreateTwoPendingTasks()
    {
        BackgroundTaskExecution.InMemoryTasks.Clear();
        using var db = CreateInMemoryDbContext();
        var queue = new DatabaseBackgroundTaskQueue(db);
        var userId = Guid.NewGuid().ToString("N");

        await queue.EmailConfirmationAsync(userId, CancellationToken.None);
        await queue.EmailConfirmationAsync(userId, CancellationToken.None);

        var tasks = BackgroundTaskExecution.InMemoryTasks.Values
            .Where(t => t.Kind == "email-verification" && t.TargetId == userId)
            .ToList();

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Null(t.CompletedAt));
    }

    [Fact]
    public async Task DuplicateWelcomeAndResizeRequests_RemainDeduplicated()
    {
        BackgroundTaskExecution.InMemoryTasks.Clear();
        using var db = CreateInMemoryDbContext();
        var queue = new DatabaseBackgroundTaskQueue(db);
        var userId = Guid.NewGuid().ToString("N");
        var imageId = Guid.NewGuid();

        // Duplicate welcome
        await queue.WelcomeAsync(userId, CancellationToken.None);
        await queue.WelcomeAsync(userId, CancellationToken.None);

        var welcomeTasks = BackgroundTaskExecution.InMemoryTasks.Values
            .Where(t => t.Kind == "welcome" && t.TargetId == userId)
            .ToList();
        Assert.Single(welcomeTasks);

        // Duplicate resize
        await queue.ResizeAsync(imageId, CancellationToken.None);
        await queue.ResizeAsync(imageId, CancellationToken.None);

        var resizeTasks = BackgroundTaskExecution.InMemoryTasks.Values
            .Where(t => t.Kind == "resize" && t.TargetId == imageId.ToString())
            .ToList();
        Assert.Single(resizeTasks);
    }

    [Fact]
    public async Task PasswordResetRequestCreatesOutboxTaskWithOnlyUserId()
    {
        BackgroundTaskExecution.InMemoryTasks.Clear();
        using var db = CreateInMemoryDbContext();
        var queue = new DatabaseBackgroundTaskQueue(db);
        var userId = Guid.NewGuid().ToString("N");

        await queue.PasswordResetEmailAsync(userId, CancellationToken.None);

        var task = Assert.Single(BackgroundTaskExecution.InMemoryTasks.Values);
        Assert.Equal("password-reset-email", task.Kind);
        Assert.Equal(userId, task.TargetId);
    }
}
