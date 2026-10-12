using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories.Queries;

public sealed class GetCategoriesQueryHandler(
    ICategoryRepository categories,
    IMemoryCache cache)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<CategoryDto>>(CategoryCache.Key, out var cached)
            && cached is not null)
            return cached;

        var result = await categories.GetAllAsync(cancellationToken);
        cache.Set(CategoryCache.Key, result, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        });
        return result;
    }
}

public static class CategoryCache
{
    public const string Key = "categories:all";
}
