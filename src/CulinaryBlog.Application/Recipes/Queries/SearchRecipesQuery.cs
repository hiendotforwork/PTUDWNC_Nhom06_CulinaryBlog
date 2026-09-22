using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Recipes.Queries;

public sealed record SearchRecipesQuery(
    string Query,
    int Page = 1,
    int PageSize = 12) : IRequest<PagedResult<RecipeSummaryDto>>;