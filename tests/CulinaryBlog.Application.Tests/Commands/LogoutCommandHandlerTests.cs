namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.Logout;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ActiveToken_RevokesAndSaves()
    {
        // Arrange
        var rawToken = "sample-active-refresh-token";
        var expectedHash = RefreshToken.HashToken(rawToken);
        var token = RefreshToken.Create("user-123", rawToken, "127.0.0.1", 7);

        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(expectedHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var command = new LogoutCommand(rawToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(r => r.GetByTokenHashAsync(expectedHash, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_MissingOrWhitespaceToken_IsNoOp(string? rawToken)
    {
        // Arrange
        var command = new LogoutCommand(rawToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenRepositoryMock.Verify(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownExpiredOrRevokedToken_IsNoOp()
    {
        // 1. Unknown token
        var unknownRaw = "unknown-token";
        var unknownHash = RefreshToken.HashToken(unknownRaw);
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(unknownHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await _handler.Handle(new LogoutCommand(unknownRaw), CancellationToken.None);

        // 2. Expired token
        var expiredRaw = "expired-token";
        var expiredHash = RefreshToken.HashToken(expiredRaw);
        var expiredToken = RefreshToken.Create("user-123", expiredRaw, "127.0.0.1", -1);
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(expiredHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        await _handler.Handle(new LogoutCommand(expiredRaw), CancellationToken.None);

        // 3. Already revoked token
        var revokedRaw = "revoked-token";
        var revokedHash = RefreshToken.HashToken(revokedRaw);
        var revokedToken = RefreshToken.Create("user-123", revokedRaw, "127.0.0.1", 7);
        revokedToken.Revoke();
        _refreshTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(revokedHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        await _handler.Handle(new LogoutCommand(revokedRaw), CancellationToken.None);

        // Assert
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
