namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IExecutionTransaction> _transactionMock = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private static RegisterCommand CreateValidCommand() =>
        new("chef@example.com", "chef_john", "Chef John", "Password123!");

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync(command.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        _userRepositoryMock.Setup(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.Author))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access_token_123");

        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh_token_abc");

        // Act
        var response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.AccessToken.Should().Be("access_token_123");
        response.RefreshToken.Should().Be("refresh_token_abc");
        response.User.Email.Should().Be(command.Email);
        response.User.UserName.Should().Be(command.UserName);
        response.User.DisplayName.Should().Be(command.DisplayName);
        response.User.Roles.Should().Contain(AppRoles.Author);

        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationUser>(), command.Password), Times.Once);
        _userRepositoryMock.Verify(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.Author), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowAuthConflictExceptionWithEmailCode()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingUser = ApplicationUser.Create("Existing", command.Email, "other_user");

        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthConflictException>();
        ex.Which.ErrorCode.Should().Be("AUTH_EMAIL_EXISTS");

        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNameAlreadyExists_ShouldThrowAuthConflictExceptionWithUserNameCode()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingUser = ApplicationUser.Create("Existing", "other@example.com", command.UserName);

        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync(command.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthConflictException>();
        ex.Which.ErrorCode.Should().Be("AUTH_USERNAME_EXISTS");

        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ShouldThrowIdentityOperationException()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync(command.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync((false, new[] { "Database write failed" }));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IdentityOperationException>();

        _userRepositoryMock.Verify(r => r.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _tokenServiceMock.Verify(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Theory]
    [InlineData("DuplicateEmail")]
    [InlineData("Email 'chef@example.com' is already taken.")]
    public async Task Handle_WhenCreateFailsWithDuplicateEmailError_ShouldThrowAuthConflictExceptionWithEmailCode(string error)
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync(command.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync((false, new[] { error }));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthConflictException>();
        ex.Which.ErrorCode.Should().Be("AUTH_EMAIL_EXISTS");
    }

    [Theory]
    [InlineData("DuplicateUserName")]
    [InlineData("User name 'chef_john' is already taken.")]
    public async Task Handle_WhenCreateFailsWithDuplicateUsernameError_ShouldThrowAuthConflictExceptionWithUsernameCode(string error)
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        _userRepositoryMock.Setup(r => r.FindByUserNameAsync(command.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync((false, new[] { error }));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AuthConflictException>();
        ex.Which.ErrorCode.Should().Be("AUTH_USERNAME_EXISTS");
    }
}
