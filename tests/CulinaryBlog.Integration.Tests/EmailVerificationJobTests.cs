namespace CulinaryBlog.Integration.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class EmailVerificationJobTests
{
    private sealed class RecordingEmailService : IEmailService
    {
        public int SendWelcomeCalls;
        public int SendConfirmationCalls;
        public string? LastEmail;
        public string? LastDisplayName;
        public string? LastLink;

        public Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default)
        {
            SendWelcomeCalls++;
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(string email, string displayName, string link, CancellationToken cancellationToken = default)
        {
            SendConfirmationCalls++;
            LastEmail = email;
            LastDisplayName = displayName;
            LastLink = link;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string displayName, string link, CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private static (ApplicationDbContext Db, UserManager<ApplicationUser> UserManager) CreateTestContext()
    {
        var dbName = "EmailVerificationJobTests_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new ApplicationDbContext(options);

        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(db);
        var identityOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        identityOptions.Value.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultProvider;

        var userManager = new UserManager<ApplicationUser>(
            userStore,
            identityOptions,
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new NullLogger<UserManager<ApplicationUser>>());

        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new DataProtectorTokenProvider<ApplicationUser>(
            new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(),
            Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
            {
                TokenLifespan = TimeSpan.FromHours(24)
            }),
            new NullLogger<DataProtectorTokenProvider<ApplicationUser>>()));

        return (db, userManager);
    }

    [Fact]
    public async Task PendingUser_GeneratesTokenAtSendTime_AndSendsEmailWithFragmentLink()
    {
        var (db, userManager) = CreateTestContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "pending@culinaryblog.vn",
            UserName = "pending_chef",
            DisplayName = "Chef Pending",
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId,
            Kind = "email-verification",
            TargetId = user.Id
        };

        var emailService = new RecordingEmailService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Site:BaseUrl"] = "http://localhost:3000"
            })
            .Build();
        var env = new TestHostEnvironment("Development");

        var job = new EmailVerificationJob(db, userManager, emailService, config, env, NullLogger<EmailVerificationJob>.Instance);
        await job.RunAsync(taskId, CancellationToken.None);

        Assert.Equal(1, emailService.SendConfirmationCalls);
        Assert.Equal(user.Email, emailService.LastEmail);
        Assert.Equal(user.DisplayName, emailService.LastDisplayName);

        var link = emailService.LastLink;
        Assert.NotNull(link);
        Assert.StartsWith("http://localhost:3000/verify-email#", link);

        var fragment = link.Substring("http://localhost:3000/verify-email#".Length);
        var queryParams = HttpUtility.ParseQueryString(fragment);
        var userId = queryParams["userId"];
        var token = queryParams["token"];

        Assert.Equal(user.Id, userId);
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Verify that the token generated at send time by the job successfully confirms the user
        var confirmResult = await userManager.ConfirmEmailAsync(user, token!);
        Assert.True(confirmResult.Succeeded);

        // Verify task completed
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
    }

    [Fact]
    public async Task ConfirmedUser_DoesNotSendEmail_AndCompletesTask()
    {
        var (db, userManager) = CreateTestContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "confirmed@culinaryblog.vn",
            UserName = "confirmed_chef",
            DisplayName = "Chef Confirmed",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId,
            Kind = "email-verification",
            TargetId = user.Id
        };

        var emailService = new RecordingEmailService();
        var config = new ConfigurationBuilder().Build();
        var env = new TestHostEnvironment("Development");

        var job = new EmailVerificationJob(db, userManager, emailService, config, env, NullLogger<EmailVerificationJob>.Instance);
        await job.RunAsync(taskId, CancellationToken.None);

        Assert.Equal(0, emailService.SendConfirmationCalls);
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
    }

    [Fact]
    public async Task MissingUser_DoesNotSendEmail_AndCompletesTask()
    {
        var (db, userManager) = CreateTestContext();
        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId,
            Kind = "email-verification",
            TargetId = Guid.NewGuid().ToString()
        };

        var emailService = new RecordingEmailService();
        var config = new ConfigurationBuilder().Build();
        var env = new TestHostEnvironment("Development");

        var job = new EmailVerificationJob(db, userManager, emailService, config, env, NullLogger<EmailVerificationJob>.Instance);
        await job.RunAsync(taskId, CancellationToken.None);

        Assert.Equal(0, emailService.SendConfirmationCalls);
        Assert.NotNull(BackgroundTaskExecution.InMemoryTasks[taskId].CompletedAt);
    }

    [Theory]
    [InlineData("Production", "http://insecure.culinaryblog.vn")] // In production, HTTP is rejected
    [InlineData("Production", null)]                              // In production, missing Site:BaseUrl is rejected
    [InlineData("Development", "not a valid absolute url")]       // Malformed URL is rejected
    public async Task InvalidBaseUrlConfiguration_ThrowsAndDoesNotSendEmail(string envName, string? baseUrl)
    {
        var (db, userManager) = CreateTestContext();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = "pending@culinaryblog.vn",
            UserName = "pending_chef",
            DisplayName = "Chef Pending",
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var taskId = Guid.NewGuid();
        BackgroundTaskExecution.InMemoryTasks[taskId] = new BackgroundTaskRow
        {
            Id = taskId,
            Kind = "email-verification",
            TargetId = user.Id
        };

        var emailService = new RecordingEmailService();
        var configData = new Dictionary<string, string?>();
        if (baseUrl != null) configData["Site:BaseUrl"] = baseUrl;
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        var env = new TestHostEnvironment(envName);

        var job = new EmailVerificationJob(db, userManager, emailService, config, env, NullLogger<EmailVerificationJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.RunAsync(taskId, CancellationToken.None));
        Assert.Equal(0, emailService.SendConfirmationCalls);
    }
}
