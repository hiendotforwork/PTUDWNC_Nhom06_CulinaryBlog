namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.Login;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IExecutionTransaction> _transactionMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private static LoginCommand CreateValidCommand() =>
        new("chef@example.com", "Password123!");

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidCredentials()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("AUTH_INVALID_CREDENTIALS");
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenPasswordIncorrect_ShouldThrowInvalidCredentials()
    {
        // Arrange
        var command = CreateValidCommand();
        var user = ApplicationUser.Create("Chef John", command.Email, "chef_john");
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.CheckPasswordSignInAsync(user, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResult(false));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("AUTH_INVALID_CREDENTIALS");
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_ShouldThrowAccountLocked()
    {
        // Arrange
        var command = CreateValidCommand();
        var user = ApplicationUser.Create("Chef John", command.Email, "chef_john");
        var lockoutEnd = DateTime.UtcNow.AddMinutes(15);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.CheckPasswordSignInAsync(user, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResult(false, IsLockedOut: true, LockoutEnd: lockoutEnd));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("AUTH_ACCOUNT_LOCKED");
        ex.Which.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ShouldRevokeOldTokensAndReturnAuthResponse()
    {
        // Arrange
        var command = CreateValidCommand();
        var user = ApplicationUser.Create("Chef John", command.Email, "chef_john");
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.CheckPasswordSignInAsync(user, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResult(true));
        _userRepositoryMock.Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.Author });

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("access_token_xyz");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh_token_uvw");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("access_token_xyz");
        response.RefreshToken.Should().Be("refresh_token_uvw");
        response.User.Email.Should().Be(command.Email);

        // Verify token revocation was called
        _refreshTokenRepositoryMock.Verify(r => r.RevokeByUserIdAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
