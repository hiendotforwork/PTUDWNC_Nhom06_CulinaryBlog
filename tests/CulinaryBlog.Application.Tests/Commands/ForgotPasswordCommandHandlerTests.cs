namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.ForgotPassword;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IBackgroundTaskQueue> _queue = new();
    private readonly Mock<IPasswordResetRequestLimiter> _limiter = new();
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _logger = new();
    private readonly ForgotPasswordCommandHandler _handler;
    private readonly ApplicationUser _user = new() { Id = "user-1", EmailConfirmed = true };

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(_users.Object, _queue.Object, _limiter.Object, _logger.Object);
        _limiter.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(x => x.HasPasswordAsync(_user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task EligibleAccountUsesNormalizedEmailForCooldownBeforeLookupAndQueuesOnce()
    {
        var callOrder = new List<string>();
        _limiter.Setup(x => x.TryAcquireAsync("user@example.com", It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("limiter"))
            .ReturnsAsync(true);
        _users.Setup(x => x.FindByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("lookup"))
            .ReturnsAsync(_user);

        await _handler.Handle(new ForgotPasswordCommand("  User@Example.COM "), CancellationToken.None);

        callOrder.Should().ContainInOrder("limiter", "lookup");
        _limiter.Verify(x => x.TryAcquireAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(x => x.FindByEmailAsync("user@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _queue.Verify(x => x.PasswordResetEmailAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnknownEmailIsNotQueued()
    {
        _users.Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        await _handler.Handle(new ForgotPasswordCommand("unknown@example.com"), CancellationToken.None);

        _queue.Verify(x => x.PasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnconfirmedEmailIsNotQueued()
    {
        _user.EmailConfirmed = false;

        await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        _queue.Verify(x => x.PasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordlessAccountIsNotQueued()
    {
        _users.Setup(x => x.HasPasswordAsync(_user, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        _queue.Verify(x => x.PasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CooldownHitSkipsLookupAndEnqueue()
    {
        _limiter.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        _users.Verify(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _queue.Verify(x => x.PasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LimiterFailureSkipsLookupAndEnqueue()
    {
        _limiter.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Redis unavailable"));

        await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        _users.Verify(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _queue.Verify(x => x.PasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueueFailureStillReturnsAcceptedResult()
    {
        _queue.Setup(x => x.PasswordResetEmailAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Outbox unavailable"));

        var result = await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void ValidatorRejectsInvalidEmail(string email)
    {
        new ForgotPasswordCommandValidator().Validate(new ForgotPasswordCommand(email)).IsValid.Should().BeFalse();
    }
}
