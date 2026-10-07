using CulinaryBlog.Application.Categories.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<CategoryDto?>;
