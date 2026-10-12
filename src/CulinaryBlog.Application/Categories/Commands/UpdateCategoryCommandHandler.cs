using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Queries;
using CulinaryBlog.Application.Categories.Repositories;
using CulinaryBlog.Application.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categories,
    IValidator<UpdateCategoryCommand> validator,
    IMemoryCache cache)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto?>
{
    public async Task<CategoryDto?> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return null;

        var name = request.Name.Trim();
        if (await categories.NameExistsAsync(
                name,
                excludingId: category.Id,
                cancellationToken: cancellationToken))
        {
            throw new CategoryConflictException("CATEGORY_NAME_EXISTS", "Tên danh mục đã tồn tại.");
        }

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        categories.Update(category);
        await categories.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryCache.Key);

        var publishedRecipeCount = await categories.CountPublishedRecipesAsync(category.Id, cancellationToken);
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            publishedRecipeCount);
    }
}
