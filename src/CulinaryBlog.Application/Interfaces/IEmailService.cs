namespace CulinaryBlog.Application.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default);
}
