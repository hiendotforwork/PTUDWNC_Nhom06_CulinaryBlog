namespace CulinaryBlog.Integration.Tests;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class PasswordResetEmailJobTests
{
    private sealed class RecordingEmailService : IEmailService
    {
        public int Calls;
        public bool Fail;
        public string? LastEmail;
        public string? LastLink;

        public Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendEmailConfirmationAsync(string email, string displayName, string link, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string displayName, string link, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastEmail = email;
            LastLink = link;
            return Fail ? Task.FromException(new IOException("mail failed")) : Task.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CulinaryBlog.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = environmentName;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
    }

    private static async Task<(ApplicationDbContext Db, IUserRepository Users, ApplicationUser User)> CreateTestContext(bool eligible = true, bool confirmed = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("PasswordResetEmailJobTests_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options);
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(db);
        var identityOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        identityOptions.Value.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
        var manager = new UserManager<ApplicationUser>(store, identityOptions, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!,
            new NullLogger<UserManager<ApplicationUser>>());
        manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new DataProtectorTokenProvider<ApplicationUser>(
            new EphemeralDataProtectionProvider(), Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()),
            new NullLogger<DataProtectorTokenProvider<ApplicationUser>>()));
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"), Email = "reset@example.com", UserName = "reset-user",
            DisplayName = "Reset User", EmailConfirmed = confirmed
        };
        if (eligible) Assert.True((await manager.CreateAsync(user, "OriginalPass123!")).Succeeded);
        else { db.Users.Add(user); await db.SaveChangesAsync(); }
        return (db, new UserRepository(manager), user);
    }

    private static PasswordResetEmailJob CreateJob(ApplicationDbContext db, IUserRepository users, RecordingEmailService email) =>
        new(db, users, email,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Site:BaseUrl"] = "http://localhost:3000"
            }).Build(),
            new TestHostEnvironment("Development"), NullLogger<PasswordResetEmailJob>.Instance);

    [Fact]
    public async Task EligibleUserGetsUsableResetFragmentAndTaskCompletes()
    {
        var (db, users, user) = await CreateTestContext();
        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId, Kind = "password-reset-email", TargetId = user.Id
        };
        var email = new RecordingEmailService();

        await CreateJob(db, users, email).RunAsync(taskId, CancellationToken.None);

        Assert.Equal(1, email.Calls);
        Assert.Equal(user.Email, email.LastEmail);
        Assert.StartsWith("http://localhost:3000/reset-password#userId=", email.LastLink);
        Assert.Contains("&token=", email.LastLink);
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
        var fragment = email.LastLink!.Split('#', 2)[1];
        var values = HttpUtility.ParseQueryString(fragment);
        Assert.Equal(user.Id, values["userId"]);
        var reset = await users.ResetPasswordAsync(user, values["token"]!, "UpdatedPass123!");
        Assert.True(reset.Succeeded);
        Assert.DoesNotContain("OriginalPass123!", email.LastLink);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task IneligibleUserDoesNotReceiveEmailAndTaskCompletes()
    {
        var (db, users, user) = await CreateTestContext(eligible: false, confirmed: true);
        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId, Kind = "password-reset-email", TargetId = user.Id
        };
        var email = new RecordingEmailService();

        await CreateJob(db, users, email).RunAsync(taskId, CancellationToken.None);

        Assert.Equal(0, email.Calls);
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task MissingUserDoesNotReceiveEmailAndTaskCompletes()
    {
        var (db, users, _) = await CreateTestContext();
        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId, Kind = "password-reset-email", TargetId = Guid.NewGuid().ToString("N")
        };
        var email = new RecordingEmailService();

        await CreateJob(db, users, email).RunAsync(taskId, CancellationToken.None);

        Assert.Equal(0, email.Calls);
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task EmailFailureLeavesTaskIncompleteForRetry()
    {
        var (db, users, user) = await CreateTestContext();
        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId, Kind = "password-reset-email", TargetId = user.Id
        };
        var email = new RecordingEmailService { Fail = true };

        await Assert.ThrowsAsync<IOException>(() => CreateJob(db, users, email).RunAsync(taskId, CancellationToken.None));

        Assert.Null(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
        await db.DisposeAsync();
    }
}
