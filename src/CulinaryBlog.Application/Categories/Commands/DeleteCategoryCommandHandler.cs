using CulinaryBlog.Application.Categories.Queries;
using CulinaryBlog.Application.Categories.Repositories;
using CulinaryBlog.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categories,
    IMemoryCache cache)
    : IRequestHandler<DeleteCategoryCommand, bool>
{
    public async Task<bool> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return false;

        var recipeCount = await categories.CountRecipesAsync(category.Id, cancellationToken);
        if (recipeCount > 0)
        {
            throw new CategoryConflictException(
                "CATEGORY_HAS_RECIPES",
                $"Không thể xóa danh mục còn chứa {recipeCount} công thức.");
        }

        categories.SoftDelete(category);
        await categories.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryCache.Key);
        return true;
    }
}
