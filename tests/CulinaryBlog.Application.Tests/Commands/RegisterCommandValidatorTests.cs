namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.Register;
using FluentAssertions;
using Xunit;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand CreateValidCommand() =>
        new("chef@example.com", "chef_john", "Chef John", "Password123!");

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    [InlineData("")]
    public void Validate_WhenUsernameIsTooShort_ShouldFail(string userName)
    {
        var command = CreateValidCommand() with { UserName = userName };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.UserName));
    }

    [Fact]
    public void Validate_WhenUsernameIsTooLong_ShouldFail()
    {
        var longUsername = new string('a', 31);
        var command = CreateValidCommand() with { UserName = longUsername };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.UserName));
    }

    [Theory]
    [InlineData("user-name")]
    [InlineData("user.name")]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user#name")]
    public void Validate_WhenUsernameHasInvalidCharacters_ShouldFail(string userName)
    {
        var command = CreateValidCommand() with { UserName = userName };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.UserName));
    }

    [Theory]
    [InlineData("chef_123")]
    [InlineData("Chef_John_99")]
    [InlineData("chef123")]
    public void Validate_WhenUsernameHasValidUnderscoreAndAlphanumeric_ShouldPass(string userName)
    {
        var command = CreateValidCommand() with { UserName = userName };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void Validate_WhenDisplayNameIsTooShort_ShouldFail(string displayName)
    {
        var command = CreateValidCommand() with { DisplayName = displayName };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.DisplayName));
    }

    [Fact]
    public void Validate_WhenDisplayNameIsTooLong_ShouldFail()
    {
        var longDisplayName = new string('a', 101);
        var command = CreateValidCommand() with { DisplayName = longDisplayName };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.DisplayName));
    }

    [Theory]
    [InlineData("Pass1!")]
    [InlineData("P1!")]
    [InlineData("")]
    public void Validate_WhenPasswordIsTooShort_ShouldFail(string password)
    {
        var command = CreateValidCommand() with { Password = password };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WhenPasswordMissingUppercase_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = "password123!" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WhenPasswordMissingLowercase_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = "PASSWORD123!" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WhenPasswordMissingDigit_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = "Password!@#" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WhenPasswordMissingSpecialChar_ShouldFail()
    {
        var command = CreateValidCommand() with { Password = "Password1234" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }
}
