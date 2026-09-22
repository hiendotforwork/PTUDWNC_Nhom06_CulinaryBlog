using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Recipes.Queries;

public sealed record GetRecipesQuery(
    int Page = 1,
    int PageSize = 12,
    Guid? CategoryId = null,
    string? Difficulty = null,
    int? MinPrepTime = null,
    int? MaxCookTime = null,
    int? MinServings = null,
    string Sort = "-createdAt") : IRequest<PagedResult<RecipeSummaryDto>>;
