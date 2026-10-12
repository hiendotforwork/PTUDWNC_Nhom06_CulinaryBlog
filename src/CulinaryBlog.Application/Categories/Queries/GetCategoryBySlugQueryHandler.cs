using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Repositories;
using MediatR;

namespace CulinaryBlog.Application.Categories.Queries;

public sealed class GetCategoryBySlugQueryHandler(ICategoryRepository categories)
    : IRequestHandler<GetCategoryBySlugQuery, CategoryDetailDto?>
{
    public Task<CategoryDetailDto?> Handle(
        GetCategoryBySlugQuery request,
        CancellationToken cancellationToken)
    {
        return categories.GetBySlugAsync(
            request.Slug,
            request.Page,
            request.PageSize,
            request.CurrentUserId,
            cancellationToken);
    }
}
