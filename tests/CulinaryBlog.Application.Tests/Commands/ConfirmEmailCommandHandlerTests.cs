namespace CulinaryBlog.Application.Tests.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Commands.Auth.ConfirmEmail;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IExecutionTransaction> _transactionMock = new();
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token-123");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("raw-refresh-token-456");

        _userRepositoryMock.Setup(r => r.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Author" });

        _handler = new ConfirmEmailCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidToken_ConfirmsEmail_AndReturnsAuthResponse_AndPersistsRefreshToken()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.ConfirmEmailAsync(user, "valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var command = new ConfirmEmailCommand(user.Id, "valid-token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("raw-refresh-token-456");
        result.User.Email.Should().Be("linh@example.com");

        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsInvalidConfirmationToken()
    {
        _userRepositoryMock.Setup(r => r.FindByIdAsync("unknown-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new ConfirmEmailCommand("unknown-id", "some-token");
        var act = () => _handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("EMAIL_CONFIRMATION_FAILED");
        ex.Which.StatusCode.Should().Be(400);

        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ThrowsEmailAlreadyConfirmed()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = true;

        _userRepositoryMock.Setup(r => r.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ConfirmEmailCommand(user.Id, "replay-token");
        var act = () => _handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("EMAIL_ALREADY_CONFIRMED");
        ex.Which.StatusCode.Should().Be(409);

        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidOrExpiredToken_ThrowsInvalidConfirmationToken()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.ConfirmEmailAsync(user, "expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, new[] { "Invalid token." }));

        var command = new ConfirmEmailCommand(user.Id, "expired-token");
        var act = () => _handler.Handle(command, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("EMAIL_CONFIRMATION_FAILED");
        ex.Which.StatusCode.Should().Be(400);

        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConcurrentConfirmation_OnlyFirstSucceeds()
    {
        var user = ApplicationUser.Create("Chef Linh", "linh@example.com", "cheflinh");
        user.EmailConfirmed = false;

        _userRepositoryMock.Setup(r => r.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var calls = 0;
        _userRepositoryMock.Setup(r => r.ConfirmEmailAsync(user, "race-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    user.EmailConfirmed = true;
                    return (true, Enumerable.Empty<string>());
                }
                return (false, new[] { "Token already consumed" });
            });

        var command = new ConfirmEmailCommand(user.Id, "race-token");
        var task1 = _handler.Handle(command, CancellationToken.None);
        var task2 = _handler.Handle(command, CancellationToken.None);

        var results = await Task.WhenAll(
            task1.ContinueWith(t => t),
            task2.ContinueWith(t => t));

        var succeeded = results.Count(t => t.Status == TaskStatus.RanToCompletion);
        var failed = results.Count(t => t.Status == TaskStatus.Faulted);

        succeeded.Should().Be(1);
        failed.Should().Be(1);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
