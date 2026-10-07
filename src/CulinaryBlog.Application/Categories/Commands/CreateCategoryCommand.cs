using CulinaryBlog.Application.Categories.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description) : IRequest<CategoryDto>;
