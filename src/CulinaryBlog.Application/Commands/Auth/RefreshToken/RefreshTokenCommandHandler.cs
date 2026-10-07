namespace CulinaryBlog.Application.Commands.Auth.RefreshToken;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshToken.HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // A1: Token not found
        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw AuthException.InvalidCredentials();
        }

        // A3: Token reuse attack detected
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("SECURITY ALERT: Refresh token reuse detected for user {UserId}", refreshToken.UserId);
            throw AuthException.TokenReuseDetected();
        }

        // A2: Token expired
        if (refreshToken.IsExpired)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}", refreshToken.UserId);
            throw AuthException.InvalidCredentials();
        }

        // A4: User deleted or locked
        var user = refreshToken.User;
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive for refresh token");
            throw AuthException.InvalidCredentials();
        }

        // Get user roles
        var roles = await _userRepository.GetRolesAsync(user);

        // Begin transaction with Serializable isolation
        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // Revoke old token with reference to new token
            var newRawToken = _tokenService.GenerateRefreshToken();
            refreshToken.Revoke(RefreshToken.HashToken(newRawToken));

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles.ToList());
            var newRefreshTokenEntity = RefreshToken.Create(
                user.Id,
                newRawToken,
                ipAddress: null,
                daysToLive: 7);

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
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
                newAccessToken,
                newRawToken,
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
