namespace CulinaryBlog.Application.Tests.Queries.Auth;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Queries.Auth;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_UserFound_MapsPublicProfileAndRoles()
    {
        var user = new ApplicationUser
        {
            Id = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com",
            UserName = "testuser",
            AvatarUrl = "https://example.com/avatar.png",
            Bio = "Culinary enthusiast",
            EmailConfirmed = true,
            CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
        };

        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author", "Reader" });
        _userRepositoryMock
            .Setup(r => r.HasPasswordAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new GetCurrentUserQuery("user-123"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-123");
        result.DisplayName.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
        result.UserName.Should().Be("testuser");
        result.AvatarUrl.Should().Be("https://example.com/avatar.png");
        result.Bio.Should().Be("Culinary enthusiast");
        result.EmailConfirmed.Should().BeTrue();
        result.HasPassword.Should().BeTrue();
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.Roles.Should().BeEquivalentTo(new[] { "Author", "Reader" });
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_ReturnsEmptyRoles()
    {
        var user = new ApplicationUser
        {
            Id = "user-123",
            DisplayName = "Test User",
            Email = "test@example.com",
            UserName = "testuser",
            AvatarUrl = null,
            Bio = null
        };

        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        _userRepositoryMock
            .Setup(r => r.HasPasswordAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new GetCurrentUserQuery("user-123"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.AvatarUrl.Should().BeNull();
        result.Bio.Should().BeNull();
        result.Roles.Should().BeEmpty();
        result.HasPassword.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "validuser")]
    [InlineData("", "validuser")]
    [InlineData("   ", "validuser")]
    [InlineData("valid@example.com", null)]
    [InlineData("valid@example.com", "")]
    [InlineData("valid@example.com", "   ")]
    public async Task Handle_UserWithoutEmailOrUserName_ThrowsInvalidOperationException(string? email, string? userName)
    {
        var user = new ApplicationUser
        {
            Id = "user-123",
            DisplayName = "Test User",
            Email = email!,
            UserName = userName!
        };

        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => _handler.Handle(new GetCurrentUserQuery("user-123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNull()
    {
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("missing-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(new GetCurrentUserQuery("missing-user"), CancellationToken.None);

        result.Should().BeNull();
        _userRepositoryMock.Verify(r => r.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}
