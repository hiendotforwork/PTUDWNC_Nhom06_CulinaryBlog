namespace CulinaryBlog.Application.DTOs.Auth;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
