namespace CulinaryBlog.Application.Commands.Auth.ResetPassword;

using MediatR;

public record ResetPasswordCommand(string UserId, string Token, string NewPassword) : IRequest<Unit>;
