namespace CulinaryBlog.Application.Commands.Auth.ResetPassword;

using System.Data;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var user = await users.FindByIdAsync(request.UserId, cancellationToken);
            if (user is null) throw AuthException.InvalidPasswordResetToken();

            var result = await users.ResetPasswordAsync(user, request.Token, request.NewPassword, cancellationToken);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code == "InvalidToken"))
                    throw AuthException.InvalidPasswordResetToken();
                throw new IdentityOperationException("Resetting password failed", result.Errors.Select(error => error.Description));
            }

            await refreshTokens.RevokeByUserIdAsync(user.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Unit.Value;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
