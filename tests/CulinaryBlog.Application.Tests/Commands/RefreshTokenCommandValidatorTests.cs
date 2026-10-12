namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.RefreshToken;
using FluentAssertions;
using Xunit;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTokenIsValid_ShouldPass()
    {
        var command = new RefreshTokenCommand("valid-refresh-token-string-12345");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WhenTokenIsEmptyOrNull_ShouldFail(string? token)
    {
        var command = new RefreshTokenCommand(token!);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    public void Validate_WhenTokenIsTooShort_ShouldFail(string token)
    {
        var command = new RefreshTokenCommand(token);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}
