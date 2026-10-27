namespace CulinaryBlog.Application.Commands.Auth.UpdateProfile;

using FluentValidation;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        When(x => x.DisplayName != null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .Must(d => d.Trim().Length >= 2 && d.Trim().Length <= 100)
                .WithMessage("DisplayName must be between 2 and 100 characters.");
        });

        When(x => x.Bio != null, () =>
        {
            RuleFor(x => x.Bio!)
                .MaximumLength(500)
                .WithMessage("Bio cannot exceed 500 characters.");
        });
    }
}
