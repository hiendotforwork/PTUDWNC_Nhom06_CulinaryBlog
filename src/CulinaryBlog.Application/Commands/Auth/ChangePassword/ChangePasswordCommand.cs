namespace CulinaryBlog.Application.Commands.Auth.ChangePassword;

using MediatR;

public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword) : IRequest<Unit>;
