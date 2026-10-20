namespace CulinaryBlog.Application.Interfaces;

using System.Threading;
using System.Threading.Tasks;

public interface IEmailConfirmationResendLimiter
{
    Task<bool> TryAcquireAsync(string normalizedEmail, CancellationToken ct = default);
}
