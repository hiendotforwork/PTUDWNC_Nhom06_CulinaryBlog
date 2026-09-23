namespace CulinaryBlog.Infrastructure.Services.Email;

using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: Sending welcome email to {Email} ({DisplayName})", email, displayName);
        return Task.CompletedTask;
    }
}
