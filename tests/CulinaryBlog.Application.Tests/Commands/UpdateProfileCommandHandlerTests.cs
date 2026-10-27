namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.UpdateProfile;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(_userRepositoryMock.Object);
    }

    private static ApplicationUser CreateTestUser(string id = "user-123") => new()
    {
        Id = id,
        DisplayName = "Original Name",
        Email = "user@example.com",
        UserName = "testuser",
        AvatarUrl = "https://example.com/avatar.jpg",
        Bio = "Original bio",
        EmailConfirmed = true,
        CreatedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task MissingUserThrowsUserNotFound()
    {
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var act = async () => await _handler.Handle(
            new UpdateProfileCommand("nonexistent", "New Name", "New Bio"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(e => e.StatusCode == 404);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PartialUpdatePreservesOmittedFieldsAndNulls()
    {
        var user = CreateTestUser("user-123");
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        _userRepositoryMock
            .Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author" });

        // Only update DisplayName, leave Bio null (omitted)
        var command = new UpdateProfileCommand("user-123", "Updated Name", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.DisplayName.Should().Be("Updated Name");
        result.Bio.Should().Be("Original bio");
        result.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        result.Email.Should().Be("user@example.com");
        result.UserName.Should().Be("testuser");

        _userRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<ApplicationUser>(u => u.Id == "user-123" && u.DisplayName == "Updated Name" && u.Bio == "Original bio"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DisplayNameIsTrimmedBeforePersistence()
    {
        var user = CreateTestUser("user-123");
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        _userRepositoryMock
            .Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author" });

        var command = new UpdateProfileCommand("user-123", "   Trimmed Name   ", null);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.DisplayName.Should().Be("Trimmed Name");

        _userRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<ApplicationUser>(u => u.DisplayName == "Trimmed Name"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EmptyBioClearsAndReturnsProfileWithRoles()
    {
        var user = CreateTestUser("user-123");
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        _userRepositoryMock
            .Setup(r => r.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Author", "Admin" });

        var command = new UpdateProfileCommand("user-123", null, "");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Bio.Should().BeNull();
        result.Roles.Should().BeEquivalentTo(new[] { "Author", "Admin" });

        _userRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<ApplicationUser>(u => u.Bio == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IdentityUpdateFailureThrowsIdentityOperationException()
    {
        var user = CreateTestUser("user-123");
        _userRepositoryMock
            .Setup(r => r.FindByIdAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, new[] { "Concurrency failure" }));

        var command = new UpdateProfileCommand("user-123", "New Name", null);
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<IdentityOperationException>()
            .Where(e => e.Errors.Contains("Concurrency failure"));
    }
}
