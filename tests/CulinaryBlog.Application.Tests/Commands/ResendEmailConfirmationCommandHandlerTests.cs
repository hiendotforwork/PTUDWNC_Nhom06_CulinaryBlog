namespace CulinaryBlog.Application.Tests.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Commands.Auth.ResendEmailConfirmation;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

public class ResendEmailConfirmationCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock = new();
    private readonly Mock<IEmailConfirmationResendLimiter> _limiterMock = new();
    private readonly ResendEmailConfirmationCommandHandler _handler;

    public ResendEmailConfirmationCommandHandlerTests()
    {
        _limiterMock.Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new ResendEmailConfirmationCommandHandler(
            _userRepositoryMock.Object,
            _backgroundTaskQueueMock.Object,
            _limiterMock.Object
        );
    }

    [Fact]
    public async Task Handle_PendingUser_NotCooldown_QueuesEmailTask_AndReturnsUnit()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByEmailAsync("linh@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ResendEmailConfirmationCommand("linh@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _backgroundTaskQueueMock.Verify(q => q.EmailConfirmationAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownEmail_NotCooldown_DoesNotQueueTask_AndReturnsUnit()
    {
        _userRepositoryMock.Setup(r => r.FindByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new ResendEmailConfirmationCommand("unknown@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _backgroundTaskQueueMock.Verify(q => q.EmailConfirmationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedUser_NotCooldown_DoesNotQueueTask_AndReturnsUnit()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = true;

        _userRepositoryMock.Setup(r => r.FindByEmailAsync("linh@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ResendEmailConfirmationCommand("linh@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _backgroundTaskQueueMock.Verify(q => q.EmailConfirmationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCooldownActive_DoesNotQueueTask_AndReturnsUnit()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByEmailAsync("linh@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _limiterMock.Setup(l => l.TryAcquireAsync("linh@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ResendEmailConfirmationCommand("linh@example.com");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _backgroundTaskQueueMock.Verify(q => q.EmailConfirmationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeCheckingLimiter()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByEmailAsync("linh@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ResendEmailConfirmationCommand("  Linh@Example.COM  ");
        await _handler.Handle(command, CancellationToken.None);

        _limiterMock.Verify(l => l.TryAcquireAsync("linh@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.FindByEmailAsync("linh@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }
}
