using FluentValidation;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Mã danh mục không hợp lệ.");

        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Tên danh mục không được để trống.")
            .Must(name => name is not null && name.Trim().Length is >= 2 and <= 50)
            .WithMessage("Tên danh mục phải từ 2 đến 50 ký tự.")
            .Must(name => name is not null && !name.Contains('<') && !name.Contains('>'))
            .WithMessage("Tên danh mục không được chứa HTML.");
    }
}
