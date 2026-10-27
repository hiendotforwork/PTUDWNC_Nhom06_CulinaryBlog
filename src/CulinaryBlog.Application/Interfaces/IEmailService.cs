namespace CulinaryBlog.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default);
    Task SendEmailConfirmationAsync(string email, string displayName, string link, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, string displayName, string link, CancellationToken cancellationToken = default);
}
