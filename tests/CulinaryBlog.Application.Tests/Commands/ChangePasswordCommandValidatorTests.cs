namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.ChangePassword;
using FluentAssertions;
using Xunit;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Theory]
    [InlineData("", "new-pass")]
    [InlineData("old-pass", "")]
    [InlineData("   ", "new-pass")]
    [InlineData("old-pass", "   ")]
    public void Handle_EmptyPasswordFields_ReturnsValidationFailure(string currentPassword, string newPassword)
    {
        var result = _validator.Validate(new ChangePasswordCommand("user-1", currentPassword, newPassword));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Handle_NonEmptyPasswords_PassesValidation()
    {
        var result = _validator.Validate(new ChangePasswordCommand("user-1", "old-pass", "new-pass"));

        result.IsValid.Should().BeTrue();
    }
}
