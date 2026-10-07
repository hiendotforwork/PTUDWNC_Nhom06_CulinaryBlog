namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.RefreshToken;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExecutionTransaction> _transactionMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _userRepoMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionMock = new Mock<IExecutionTransaction>();
        var loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepoMock.Object,
            _tokenServiceMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAuthResponse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var rawToken = "valid-refresh-token";
        var user = ApplicationUser.Create("Test User", "test@test.com", "testuser");
        user.Id = userId;
        user.IsActive = true;

        var refreshToken = RefreshToken.Create(userId, rawToken, daysToLive: 7);
        refreshToken.User = user;

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(refreshToken);
        _userRepoMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author" });
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new-raw-token");
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("new-access-token");

        // Act
        var result = await _handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-token");
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _handler.Handle(new RefreshTokenCommand("invalid-token"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_TokenRevoked_ThrowsSecurityException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        var token = RefreshToken.Create(userId, "token", daysToLive: 7);
        token.User = user;
        token.Revoke(); // Mark as revoked

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act
        var act = () => _handler.Handle(new RefreshTokenCommand("revoked-token"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("AUTH_TOKEN_REUSE");
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_TokenExpired_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshToken.HashToken("expired-token"),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            User = user
        };

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act
        var act = () => _handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_UserInactive_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = ApplicationUser.Create("Test", "test@test.com", "test");
        user.Id = userId;
        user.IsActive = false; // Inactive

        var token = RefreshToken.Create(userId, "valid-token", daysToLive: 7);
        token.User = user;

        _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        // Act
        var act = () => _handler.Handle(new RefreshTokenCommand("valid-token"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.StatusCode.Should().Be(401);
    }
}
