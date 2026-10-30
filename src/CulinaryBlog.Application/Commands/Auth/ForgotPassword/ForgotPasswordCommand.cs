namespace CulinaryBlog.Application.Commands.Auth.ForgotPassword;

using MediatR;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
