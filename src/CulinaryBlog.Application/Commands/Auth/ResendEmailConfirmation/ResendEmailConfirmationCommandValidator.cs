namespace CulinaryBlog.Application.Commands.Auth.ResendEmailConfirmation;

using FluentValidation;

public class ResendEmailConfirmationCommandValidator : AbstractValidator<ResendEmailConfirmationCommand>
{
    public ResendEmailConfirmationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Vui lòng nhập email hợp lệ.");
    }
}
