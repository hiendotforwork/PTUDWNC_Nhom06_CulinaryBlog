namespace CulinaryBlog.Application.Commands.Auth.Register;

using System.Text.RegularExpressions;
using FluentValidation;

public partial class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    [GeneratedRegex("^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")]
    private static partial Regex SpecialCharRegex();

    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Vui lòng nhập email hợp lệ.");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .Length(3, 30).WithMessage("Tên đăng nhập từ 3 đến 30 ký tự.")
            .Must(u => !string.IsNullOrEmpty(u) && UsernameRegex().IsMatch(u))
            .WithMessage("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_).");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .Length(2, 100).WithMessage("Tên hiển thị từ 2 đến 100 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có tối thiểu 8 ký tự.")
            .Must(p => !string.IsNullOrEmpty(p) && UppercaseRegex().IsMatch(p))
            .WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Must(p => !string.IsNullOrEmpty(p) && LowercaseRegex().IsMatch(p))
            .WithMessage("Mật khẩu phải chứa ít nhất 1 chữ thường.")
            .Must(p => !string.IsNullOrEmpty(p) && DigitRegex().IsMatch(p))
            .WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.")
            .Must(p => !string.IsNullOrEmpty(p) && SpecialCharRegex().IsMatch(p))
            .WithMessage("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.");
    }
}
