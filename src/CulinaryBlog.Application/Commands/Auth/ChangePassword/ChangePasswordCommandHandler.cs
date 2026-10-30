namespace CulinaryBlog.Application.Commands.Auth.ChangePassword;

using System.Data;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var user = await users.FindByIdAsync(request.UserId, cancellationToken);
            if (user is null) throw AuthException.UserNotFound();

            var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword, cancellationToken);
            if (!result.Succeeded)
                throw new IdentityOperationException("Changing password failed", result.Errors.Select(error => error.Description));

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
