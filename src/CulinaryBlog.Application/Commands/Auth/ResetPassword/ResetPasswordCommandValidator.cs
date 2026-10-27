namespace CulinaryBlog.Application.Commands.Auth.ResetPassword;

using FluentValidation;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.NewPassword).NotEmpty();
    }
}
