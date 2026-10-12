namespace CulinaryBlog.Application.Commands.Auth.Logout;

using MediatR;

public record LogoutCommand(string? RefreshToken) : IRequest;
