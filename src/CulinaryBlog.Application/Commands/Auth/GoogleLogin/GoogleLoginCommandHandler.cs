namespace CulinaryBlog.Application.Commands.Auth.GoogleLogin;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private const string DefaultRole = "Author";

    public GoogleLoginCommandHandler(
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

    public async Task<AuthResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user exists with this external login
        var existingUser = await _userRepository.FindByLoginAsync(request.Provider, request.ProviderKey, cancellationToken);

        // 2. If not found by login, check by email
        if (existingUser == null)
        {
            existingUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);

            if (existingUser != null)
            {
                // Link Google login to existing account
                await _userRepository.AddLoginAsync(
                    existingUser,
                    request.Provider,
                    request.ProviderKey,
                    request.DisplayName,
                    request.AvatarUrl
                );
            }
        }

        // 3. If still not found, create new user
        if (existingUser == null)
        {
            var userName = GenerateUserName(request.Email);

            existingUser = ApplicationUser.Create(request.DisplayName, request.Email, userName);

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                existingUser.AvatarUrl = request.AvatarUrl;
            }

            var createResult = await _userRepository.CreateAsync(existingUser);

            if (!createResult.Succeeded)
            {
                throw AuthException.InvalidCredentials();
            }

            // Add to Author role
            await _userRepository.AddToRoleAsync(existingUser, DefaultRole);

            // Link external login
            await _userRepository.AddLoginAsync(
                existingUser,
                request.Provider,
                request.ProviderKey,
                request.DisplayName,
                request.AvatarUrl
            );
        }

        // 4. Check if user is active
        if (!existingUser.IsActive)
        {
            throw AuthException.AccountLocked(DateTime.UtcNow.AddDays(30));
        }

        // 5. Get user roles
        var roles = await _userRepository.GetRolesAsync(existingUser);

        // 6. Begin transaction for token rotation
        await using var transaction = await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            // 7. Revoke all existing refresh tokens
            await _refreshTokenRepository.RevokeByUserIdAsync(existingUser.Id, cancellationToken);

            // 8. Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(existingUser, roles.ToList());
            var rawRefreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = RefreshToken.Create(
                existingUser.Id,
                rawRefreshToken,
                ipAddress: null,
                daysToLive: 7
            );

            // 9. Persist refresh token
            await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // 10. Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // 11. Return AuthResponse
            var userDto = new UserDto(
                existingUser.Id,
                existingUser.Email ?? request.Email,
                existingUser.UserName ?? string.Empty,
                existingUser.DisplayName,
                existingUser.AvatarUrl,
                existingUser.Bio,
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

    private static string GenerateUserName(string email)
    {
        var userName = email.Split('@')[0];
        userName = new string(userName
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .ToArray());

        if (string.IsNullOrEmpty(userName))
        {
            userName = "user";
        }

        if (userName.Length < 3)
        {
            userName = userName + "123";
        }

        return userName.ToLowerInvariant();
    }
}
