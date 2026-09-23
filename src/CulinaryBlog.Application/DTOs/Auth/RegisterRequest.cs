namespace CulinaryBlog.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string UserName,
    string DisplayName,
    string Password
);
