namespace CulinaryBlog.Application.DTOs.Auth;

public record UserDto(
    string Id,
    string Email,
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IList<string> Roles
);
