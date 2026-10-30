namespace CulinaryBlog.Application.Tests.Commands;

using System.Data;
using CulinaryBlog.Application.Commands.Auth.ResetPassword;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class ResetPasswordCommandHandlerTests
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
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>())).ReturnsAsync(_transaction);
        _users.Setup(x => x.FindByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _users.Setup(x => x.ResetPasswordAsync(_user, "valid-token", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(true, Array.Empty<UserOperationError>()));
        _refreshTokens.Setup(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new ResetPasswordCommandHandler(_users.Object, _refreshTokens.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task ValidResetRevokesRefreshTokensAndCommitsSerializableTransaction()
    {
        await _handler.Handle(new ResetPasswordCommand("user-1", "valid-token", "new-pass"), CancellationToken.None);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Committed.Should().BeTrue();
        _transaction.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidResetTokenReturnsGenericErrorAndDoesNotRevokeSessions()
    {
        _users.Setup(x => x.ResetPasswordAsync(_user, "valid-token", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(false, [new UserOperationError("InvalidToken", "Token invalid for user.")]));

        var act = () => _handler.Handle(new ResetPasswordCommand("user-1", "valid-token", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(error => error.ErrorCode == "AUTH_PASSWORD_RESET_INVALID" && error.StatusCode == 400);
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasswordPolicyFailureDoesNotRevokeSessions()
    {
        _users.Setup(x => x.ResetPasswordAsync(_user, "valid-token", "new-pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserOperationResult(false, [new UserOperationError("PasswordTooShort", "Password too short.")]));

        var act = () => _handler.Handle(new ResetPasswordCommand("user-1", "valid-token", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<IdentityOperationException>();
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingUserReturnsGenericInvalidLinkError()
    {
        _users.Setup(x => x.FindByIdAsync("user-1", It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        var act = () => _handler.Handle(new ResetPasswordCommand("user-1", "valid-token", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(error => error.ErrorCode == "AUTH_PASSWORD_RESET_INVALID" && error.StatusCode == 400);
        _transaction.RolledBack.Should().BeTrue();
        _refreshTokens.Verify(x => x.RevokeByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshRevocationFailureRollsBackReset()
    {
        _refreshTokens.Setup(x => x.RevokeByUserIdAsync("user-1", It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("database failure"));

        var act = () => _handler.Handle(new ResetPasswordCommand("user-1", "valid-token", "new-pass"), CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        _transaction.RolledBack.Should().BeTrue();
        _transaction.Committed.Should().BeFalse();
    }

    [Fact]
    public void ValidatorRejectsMissingResetFields()
    {
        var result = new ResetPasswordCommandValidator().Validate(new ResetPasswordCommand("", "", ""));

        result.IsValid.Should().BeFalse();
    }
}
