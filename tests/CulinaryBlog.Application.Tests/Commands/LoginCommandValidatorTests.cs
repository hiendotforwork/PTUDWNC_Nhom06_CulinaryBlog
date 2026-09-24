namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.Login;
using FluentAssertions;
using Xunit;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand CreateValidCommand() =>
        new("chef@example.com", "Password123!");

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = CreateValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public void Validate_WhenEmailIsInvalid_ShouldFail(string email)
    {
        var command = CreateValidCommand() with { Email = email };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenPasswordIsEmpty_ShouldFail(string password)
    {
        var command = CreateValidCommand() with { Password = password };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
