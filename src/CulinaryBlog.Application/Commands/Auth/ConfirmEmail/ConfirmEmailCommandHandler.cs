namespace CulinaryBlog.Application.Commands.Auth.ConfirmEmail;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw AuthException.InvalidConfirmationToken();
        }

        if (user.EmailConfirmed)
        {
            throw AuthException.EmailAlreadyConfirmed();
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            var (succeeded, errors) = await _userRepository.ConfirmEmailAsync(user, request.Token, cancellationToken);
            if (!succeeded)
            {
                throw AuthException.InvalidConfirmationToken();
            }

            var roles = await _userRepository.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles.ToList());
            var rawRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenEntity = RefreshToken.Create(user.Id, rawRefreshToken, ipAddress: null, daysToLive: 7);

            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var userDto = new UserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                user.Bio,
                roles.ToList());

            return new AuthResponse(
                accessToken,
                rawRefreshToken,
                DateTime.UtcNow.AddMinutes(15),
                userDto);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
