namespace CulinaryBlog.Application.Commands.Auth.RefreshToken;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
