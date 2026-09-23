namespace CulinaryBlog.Application.Commands.Auth.Register;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record RegisterCommand(
    string Email,
    string UserName,
    string DisplayName,
    string Password
) : IRequest<AuthResponse>;
