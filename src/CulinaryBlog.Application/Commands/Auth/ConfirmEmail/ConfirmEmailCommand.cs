namespace CulinaryBlog.Application.Commands.Auth.ConfirmEmail;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<AuthResponse>;
