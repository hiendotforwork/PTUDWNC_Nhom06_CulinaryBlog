namespace CulinaryBlog.Application.Tests.Commands;

using CulinaryBlog.Application.Commands.Auth.UpdateProfile;
using FluentAssertions;
using Xunit;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void DisplayNameMustBe2To100WhenSupplied()
    {
        // null passes
        _validator.Validate(new UpdateProfileCommand("user-1", null, null)).IsValid.Should().BeTrue();

        // 2 chars passes
        _validator.Validate(new UpdateProfileCommand("user-1", "ab", null)).IsValid.Should().BeTrue();

        // 100 chars passes
        _validator.Validate(new UpdateProfileCommand("user-1", new string('a', 100), null)).IsValid.Should().BeTrue();

        // 1 char fails
        var res1 = _validator.Validate(new UpdateProfileCommand("user-1", "a", null));
        res1.IsValid.Should().BeFalse();
        res1.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));

        // 101 chars fails
        var res101 = _validator.Validate(new UpdateProfileCommand("user-1", new string('a', 101), null));
        res101.IsValid.Should().BeFalse();
        res101.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));

        // Trimmed < 2 fails
        var resTrimmed = _validator.Validate(new UpdateProfileCommand("user-1", " a ", null));
        resTrimmed.IsValid.Should().BeFalse();
        resTrimmed.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.DisplayName));
    }

    [Fact]
    public void BioMayBeEmptyButCannotExceed500()
    {
        // null passes
        _validator.Validate(new UpdateProfileCommand("user-1", null, null)).IsValid.Should().BeTrue();

        // empty passes
        _validator.Validate(new UpdateProfileCommand("user-1", null, "")).IsValid.Should().BeTrue();

        // 500 chars passes
        _validator.Validate(new UpdateProfileCommand("user-1", null, new string('b', 500))).IsValid.Should().BeTrue();

        // 501 chars fails
        var res501 = _validator.Validate(new UpdateProfileCommand("user-1", null, new string('b', 501)));
        res501.IsValid.Should().BeFalse();
        res501.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.Bio));
    }
}
