namespace CulinaryBlog.Infrastructure.Services.Email;

using System.Net;
using System.Net.Mail;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendWelcomeEmailAsync(string email, string displayName, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Configure Email:SmtpHost before sending email.");
        var url = configuration["Site:BaseUrl"] ?? "http://localhost:3000";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var site) || site.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Site:BaseUrl must be HTTP(S).");
        using var message = new MailMessage(configuration["Email:From"] ?? "noreply@culinary.local", email) {
            Subject = "Chào mừng đến với Culinary Blog",
            IsBodyHtml = true,
            Body = $"<h1>Xin chào {WebUtility.HtmlEncode(displayName)}!</h1><p>Chào mừng bạn đến với Culinary Blog.</p>" +
                $"<p><a href=\"{WebUtility.HtmlEncode(url)}\">Khám phá công thức nấu ăn</a></p>"
        };
        using var smtp = new SmtpClient(host, configuration.GetValue("Email:SmtpPort", 25)) {
            EnableSsl = configuration.GetValue("Email:UseSsl", false),
            Timeout = 15000
        };
        var user = configuration["Email:UserName"];
        if (!string.IsNullOrWhiteSpace(user)) smtp.Credentials = new NetworkCredential(user, configuration["Email:Password"]);
        await smtp.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Welcome email delivered");
    }

    public async Task SendEmailConfirmationAsync(string email, string displayName, string link, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Configure Email:SmtpHost before sending email.");
        using var message = new MailMessage(configuration["Email:From"] ?? "noreply@culinary.local", email) {
            Subject = "Xác nhận địa chỉ email - Culinary Blog",
            IsBodyHtml = true,
            Body = $"<h1>Xin chào {WebUtility.HtmlEncode(displayName)}!</h1>" +
                $"<p>Cảm ơn bạn đã đăng ký tài khoản tại Culinary Blog. Vui lòng bấm vào liên kết bên dưới để xác nhận email của bạn:</p>" +
                $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Xác nhận email</a></p>" +
                $"<p>Liên kết có hiệu lực trong vòng 24 giờ.</p>"
        };
        using var smtp = new SmtpClient(host, configuration.GetValue("Email:SmtpPort", 25)) {
            EnableSsl = configuration.GetValue("Email:UseSsl", false),
            Timeout = 15000
        };
        var user = configuration["Email:UserName"];
        if (!string.IsNullOrWhiteSpace(user)) smtp.Credentials = new NetworkCredential(user, configuration["Email:Password"]);
        await smtp.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Confirmation email delivered to recipient");
    }

    public async Task SendPasswordResetAsync(string email, string displayName, string link, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("Configure Email:SmtpHost before sending email.");
        using var message = new MailMessage(configuration["Email:From"] ?? "noreply@culinary.local", email) {
            Subject = "Đặt lại mật khẩu - Culinary Blog",
            IsBodyHtml = true,
            Body = $"<h1>Xin chào {WebUtility.HtmlEncode(displayName)}!</h1>" +
                $"<p>Nhấn vào liên kết bên dưới để đặt lại mật khẩu:</p>" +
                $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Đặt lại mật khẩu</a></p>" +
                "<p>Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>"
        };
        using var smtp = new SmtpClient(host, configuration.GetValue("Email:SmtpPort", 25)) {
            EnableSsl = configuration.GetValue("Email:UseSsl", false),
            Timeout = 15000
        };
        var user = configuration["Email:UserName"];
        if (!string.IsNullOrWhiteSpace(user)) smtp.Credentials = new NetworkCredential(user, configuration["Email:Password"]);
        await smtp.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Password reset email delivered to recipient");
    }
}
