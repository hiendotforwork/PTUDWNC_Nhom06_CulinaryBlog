using CulinaryBlog.Application.Categories.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Categories.Queries;

public sealed record GetCategoryBySlugQuery(
    string Slug,
    int Page,
    int PageSize,
    string? CurrentUserId) : IRequest<CategoryDetailDto?>;
