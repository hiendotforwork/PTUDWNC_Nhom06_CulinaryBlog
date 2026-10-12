using MediatR;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<bool>;
