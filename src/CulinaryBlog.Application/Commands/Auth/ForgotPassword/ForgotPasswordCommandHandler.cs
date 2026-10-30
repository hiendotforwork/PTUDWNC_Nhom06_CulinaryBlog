namespace CulinaryBlog.Application.Commands.Auth.ForgotPassword;

using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IBackgroundTaskQueue backgroundTasks,
    IPasswordResetRequestLimiter limiter,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        bool acquired;
        try
        {
            acquired = await limiter.TryAcquireAsync(email, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Unit.Value;
        }

        if (!acquired) return Unit.Value;
        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null || !user.EmailConfirmed || !await users.HasPasswordAsync(user, cancellationToken))
            return Unit.Value;

        try
        {
            await backgroundTasks.PasswordResetEmailAsync(user.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to enqueue password reset email for user {UserId}", user.Id);
        }

        return Unit.Value;
    }
}
