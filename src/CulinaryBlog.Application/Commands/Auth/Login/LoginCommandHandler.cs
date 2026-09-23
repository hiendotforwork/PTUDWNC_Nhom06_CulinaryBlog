namespace CulinaryBlog.Application.Commands.Auth.Login;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly ApplicationUser DummyUser = ApplicationUser.Create(
        "Security Dummy",
        "security-dummy@culinaryblog.vn",
        "security_dummy");

    public LoginCommandHandler(
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

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);

        // 2. Generic error for security (no user enumeration & timing attack mitigation)
        // Return same error whether email exists or not, with constant-time password check
        if (user == null)
        {
            await _userRepository.CheckPasswordSignInAsync(DummyUser, request.Password, cancellationToken);
            throw AuthException.InvalidCredentials();
        }

        // 3. Verify password (this handles lockout automatically)
        var signInResult = await _userRepository.CheckPasswordSignInAsync(user, request.Password, cancellationToken);

        // 4. Check lockout status
        if (signInResult.IsLockedOut)
        {
            var unlockAt = signInResult.LockoutEnd ?? DateTime.UtcNow.AddMinutes(15);
            throw AuthException.AccountLocked(unlockAt);
        }

        // 5. Check if not allowed (e.g., email not confirmed)
        if (signInResult.IsNotAllowed || !signInResult.Succeeded)
        {
            throw AuthException.InvalidCredentials();
        }

        // 6. Get user roles
        var roles = await _userRepository.GetRolesAsync(user);

        // 7. Begin transaction with Serializable isolation level to eliminate concurrent token race condition
        await using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // 8. Revoke all existing refresh tokens (token rotation)
            await _refreshTokenRepository.RevokeByUserIdAsync(user.Id, cancellationToken);

            // 9. Generate new access token
            var accessToken = _tokenService.GenerateAccessToken(user, roles.ToList());

            // 10. Generate new refresh token
            var rawRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenEntity = RefreshToken.Create(user.Id, rawRefreshToken, ipAddress: null, daysToLive: 7);

            // 11. Persist new refresh token
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // 12. Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // 13. Return AuthResponse
            var userDto = new UserDto(
                user.Id,
                user.Email ?? request.Email,
                user.UserName ?? string.Empty,
                user.DisplayName,
                user.AvatarUrl,
                user.Bio,
                roles.ToList()
            );

            return new AuthResponse(
                accessToken,
                rawRefreshToken,
                DateTime.UtcNow.AddMinutes(15),
                userDto
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
