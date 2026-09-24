namespace CulinaryBlog.Application.Commands.Auth.Login;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;
