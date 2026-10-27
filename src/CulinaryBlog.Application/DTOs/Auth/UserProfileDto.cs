namespace CulinaryBlog.Application.DTOs.Auth;

public record UserProfileDto(
    string Id,
    string DisplayName,
    string Email,
    string UserName,
    string? AvatarUrl,
    string? Bio,
    string[] Roles,
    bool EmailConfirmed,
    DateTime CreatedAt
);
