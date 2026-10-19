namespace CulinaryBlog.Application.Commands.Auth.UpdateProfile;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record UpdateProfileCommand(
    string UserId,
    string? DisplayName,
    string? Bio
) : IRequest<UserProfileDto>;
