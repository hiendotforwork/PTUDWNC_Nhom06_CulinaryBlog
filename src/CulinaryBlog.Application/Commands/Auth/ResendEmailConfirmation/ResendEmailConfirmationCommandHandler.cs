namespace CulinaryBlog.Application.Commands.Auth.ResendEmailConfirmation;

using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IEmailConfirmationResendLimiter _resendLimiter;

    public ResendEmailConfirmationCommandHandler(
        IUserRepository userRepository,
        IBackgroundTaskQueue backgroundTaskQueue,
        IEmailConfirmationResendLimiter resendLimiter)
    {
        _userRepository = userRepository;
        _backgroundTaskQueue = backgroundTaskQueue;
        _resendLimiter = resendLimiter;
    }

    public async Task<Unit> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        var acquired = await _resendLimiter.TryAcquireAsync(normalizedEmail, cancellationToken);
        if (!acquired)
        {
            return Unit.Value;
        }

        var user = await _userRepository.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || user.EmailConfirmed)
        {
            return Unit.Value;
        }

        await _backgroundTaskQueue.EmailConfirmationAsync(user.Id, cancellationToken);
        return Unit.Value;
    }
}
