using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using CulinaryBlog.Application.Recipes.Repositories;
using MediatR;

namespace CulinaryBlog.Application.Recipes.Queries;

public sealed class SearchRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IRequestHandler<SearchRecipesQuery, PagedResult<RecipeSummaryDto>>
{
    public Task<PagedResult<RecipeSummaryDto>> Handle(
        SearchRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return recipeRepository.SearchPublishedAsync(request, cancellationToken);
    }
}