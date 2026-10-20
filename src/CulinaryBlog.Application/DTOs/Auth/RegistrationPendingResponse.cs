namespace CulinaryBlog.Application.DTOs.Auth;

public record RegistrationPendingResponse(string Email, bool EmailConfirmationRequired = true);
