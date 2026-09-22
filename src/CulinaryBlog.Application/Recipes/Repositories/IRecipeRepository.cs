using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using CulinaryBlog.Application.Recipes.Queries;

namespace CulinaryBlog.Application.Recipes.Repositories;

public interface IRecipeRepository
{
    Task<PagedResult<RecipeSummaryDto>> GetPublishedAsync(
        GetRecipesQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RecipeSummaryDto>> SearchPublishedAsync(
        SearchRecipesQuery query,
        CancellationToken cancellationToken = default);
}
