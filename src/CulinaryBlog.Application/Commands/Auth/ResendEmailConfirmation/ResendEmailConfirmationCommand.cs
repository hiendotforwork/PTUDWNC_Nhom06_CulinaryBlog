namespace CulinaryBlog.Application.Commands.Auth.ResendEmailConfirmation;

using MediatR;

public record ResendEmailConfirmationCommand(string Email) : IRequest<Unit>;
