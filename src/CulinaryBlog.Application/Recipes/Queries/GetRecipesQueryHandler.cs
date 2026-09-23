using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using CulinaryBlog.Application.Recipes.Repositories;
using MediatR;

namespace CulinaryBlog.Application.Recipes.Queries;

public sealed class GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IRequestHandler<GetRecipesQuery, PagedResult<RecipeSummaryDto>>
{
    public Task<PagedResult<RecipeSummaryDto>> Handle(
        GetRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return recipeRepository.GetPublishedAsync(request, cancellationToken);
    }
}
