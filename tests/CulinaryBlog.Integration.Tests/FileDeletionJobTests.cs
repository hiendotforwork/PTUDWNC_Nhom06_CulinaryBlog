using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Integration.Tests;

public sealed class FileDeletionJobTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PreservesReferencedAvatarAndDeletesAfterReferenceIsRemoved()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = (TestFileStorage)scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var url = "http://storage.test/avatars/" + Guid.NewGuid() + "/test.png";
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), AvatarUrl = url };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        storage.Files[url] = [1, 2, 3];
        var job = new FileDeletionJob(db, storage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.DeleteAsync(url, default));
        Assert.True(storage.Files.ContainsKey(url));
        user.AvatarUrl = null;
        await db.SaveChangesAsync();
        await job.DeleteAsync(url, default);
        await job.DeleteAsync(url, default);
        Assert.False(storage.Files.ContainsKey(url));
    }

    [Fact]
    public void FailedJobsRetryThreeTimes()
    {
        var method = typeof(FileDeletionJob).GetMethod(nameof(FileDeletionJob.DeleteAsync))!;
        var policy = Assert.IsType<AutomaticRetryAttribute>(Attribute.GetCustomAttribute(method, typeof(AutomaticRetryAttribute)));
        Assert.Equal(3, policy.Attempts);
    }
}
