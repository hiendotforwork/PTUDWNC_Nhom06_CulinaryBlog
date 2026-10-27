namespace CulinaryBlog.Application.Tests.Commands;

using System.Data;
using CulinaryBlog.Application.Commands.Auth.ChangePassword;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class ChangePasswordCommandHandlerTests
{
    private sealed class TestTransaction : IExecutionTransaction
    {
        public bool Committed;
        public bool RolledBack;
        public Task CommitAsync(CancellationToken cancellationToken = default) { Committed = true; return Task.CompletedTask; }
        public Task RollbackAsync(CancellationToken cancellationToken = default) { RolledBack = true; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TestTransaction _transaction = new();
    private readonly ApplicationUser _user = new() { Id = "user-1" };
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction);
        _users.Setup(x => x.FindByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(x => x.ChangePasswordAsync(_user, "old-pass", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(true, Array.Empty<UserOperationError>()));
        _refreshTokens.Setup(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _handler = new ChangePasswordCommandHandler(_users.Object, _refreshTokens.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task ValidChangeRevokesRefreshTokensAndCommitsSerializableTransaction()
    {
        await _handler.Handle(new ChangePasswordCommand("user-1", "old-pass", "new-pass"), CancellationToken.None);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Committed.Should().BeTrue();
        _transaction.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task WrongCurrentPasswordRollsBackWithoutRevokingSessions()
    {
        _users.Setup(x => x.ChangePasswordAsync(_user, "old-pass", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(false, [new UserOperationError("PasswordMismatch", "Current password is incorrect.")]));

        var act = () => _handler.Handle(new ChangePasswordCommand("user-1", "old-pass", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<IdentityOperationException>();
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordPolicyFailureRollsBackWithoutRevokingSessions()
    {
        _users.Setup(x => x.ChangePasswordAsync(_user, "old-pass", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(false, [new UserOperationError("PasswordTooShort", "Password too short.")]));

        var act = () => _handler.Handle(new ChangePasswordCommand("user-1", "old-pass", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<IdentityOperationException>();
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingUserRollsBackWithoutRevokingSessions()
    {
        _users.Setup(x => x.FindByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        var act = () => _handler.Handle(new ChangePasswordCommand("user-1", "old-pass", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>().Where(error => error.StatusCode == 404);
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshRevocationFailureRollsBackPasswordChange()
    {
        _refreshTokens.Setup(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("database failure"));

        var act = () => _handler.Handle(new ChangePasswordCommand("user-1", "old-pass", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        _transaction.RolledBack.Should().BeTrue();
        _transaction.Committed.Should().BeFalse();
    }
}
