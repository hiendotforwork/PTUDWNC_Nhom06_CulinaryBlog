namespace CulinaryBlog.Application.Interfaces;

public interface IPasswordResetRequestLimiter
{
    Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default);
}
