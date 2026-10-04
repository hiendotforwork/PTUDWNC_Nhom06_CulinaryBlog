namespace CulinaryBlog.Application.Commands.Auth.GoogleLogin;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record GoogleLoginCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string DisplayName,
    string? AvatarUrl
) : IRequest<AuthResponse>;
