namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.GoogleLogin;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class GoogleLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IExecutionTransaction> _transactionMock = new();
    private readonly GoogleLoginCommandHandler _handler;

    public GoogleLoginCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _handler = new GoogleLoginCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private static GoogleLoginCommand CreateValidCommand() =>
        new("Google", "google-sub-123", "chef@example.com", "Chef John", "https://avatar.url/john.png");

    [Fact]
    public async Task Handle_WhenUserExistsWithLogin_ReturnsAuthResponseAndRotatesTokens()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingUser = ApplicationUser.Create(command.DisplayName, command.Email, "chef_john");
        _userRepositoryMock.Setup(r => r.FindByLoginAsync(command.Provider, command.ProviderKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { "Author" });

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(existingUser, It.IsAny<List<string>>()))
            .Returns("access-token-123");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-123");
        result.User.Email.Should().Be(command.Email);
        result.User.Roles.Should().Contain("Author");

        _refreshTokenRepositoryMock.Verify(r => r.RevokeByUserIdAsync(existingUser.Id, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserExistsWithEmail_LinksLoginAndReturnsAuthResponse()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingUser = ApplicationUser.Create(command.DisplayName, command.Email, "chef_john");
        _userRepositoryMock.Setup(r => r.FindByLoginAsync(command.Provider, command.ProviderKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(r => r.AddLoginAsync(existingUser, command.Provider, command.ProviderKey, command.DisplayName, command.AvatarUrl))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.GetRolesAsync(existingUser))
            .ReturnsAsync(new List<string> { "Author" });

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(existingUser, It.IsAny<List<string>>()))
            .Returns("access-token-123");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.AddLoginAsync(existingUser, command.Provider, command.ProviderKey, command.DisplayName, command.AvatarUrl), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewUser_CreatesUserWithAuthorRoleAndLinksLogin()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByLoginAsync(command.Provider, command.ProviderKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Author"))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.AddLoginAsync(It.IsAny<ApplicationUser>(), command.Provider, command.ProviderKey, command.DisplayName, command.AvatarUrl))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Author" });

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<List<string>>()))
            .Returns("access-token-123");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.CreateAsync(It.Is<ApplicationUser>(u => u.Email == command.Email)), Times.Once);
        _userRepositoryMock.Verify(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Author"), Times.Once);
        _userRepositoryMock.Verify(r => r.AddLoginAsync(It.IsAny<ApplicationUser>(), command.Provider, command.ProviderKey, command.DisplayName, command.AvatarUrl), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ThrowsAccountLocked()
    {
        // Arrange
        var command = CreateValidCommand();
        var lockedUser = ApplicationUser.Create(command.DisplayName, command.Email, "chef_john");
        lockedUser.IsActive = false;

        _userRepositoryMock.Setup(r => r.FindByLoginAsync(command.Provider, command.ProviderKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedUser);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthException>();
        ex.Which.ErrorCode.Should().Be("AUTH_ACCOUNT_LOCKED");
        ex.Which.StatusCode.Should().Be(423);
    }

    [Fact]
    public async Task Handle_WhenNewUserHasDuplicateUserName_GeneratesUniqueUserNameAndSucceeds()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByLoginAsync(command.Provider, command.ProviderKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // First call for "chef" says username is taken
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync("chef", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationUser.Create("Other Chef", "other@example.com", "chef"));

        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Author"))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.AddLoginAsync(It.IsAny<ApplicationUser>(), command.Provider, command.ProviderKey, command.DisplayName, command.AvatarUrl))
            .ReturnsAsync((true, Enumerable.Empty<string>()));
        _userRepositoryMock.Setup(r => r.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Author" });

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<List<string>>()))
            .Returns("access-token-123");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.CreateAsync(It.Is<ApplicationUser>(u => u.UserName != "chef" && u.UserName.StartsWith("chef"))), Times.Once);
    }
}
