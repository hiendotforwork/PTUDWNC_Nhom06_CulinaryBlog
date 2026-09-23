namespace CulinaryBlog.Application.Commands.Auth.Register;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Check email uniqueness
        var existingEmailUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingEmailUser != null)
        {
            throw AuthConflictException.EmailExists();
        }

        // 2. Check username uniqueness
        var existingNameUser = await _userRepository.FindByUserNameAsync(request.UserName, cancellationToken);
        if (existingNameUser != null)
        {
            throw AuthConflictException.UserNameExists();
        }

        // 3. Create user entity
        var user = ApplicationUser.Create(request.DisplayName, request.Email, request.UserName);

        // 4. Save user with hashed password
        var (createSucceeded, createErrors) = await _userRepository.CreateAsync(user, request.Password);
        if (!createSucceeded)
        {
            throw new IdentityOperationException("User creation failed", createErrors);
        }

        // 5. Assign default Author role
        var (roleSucceeded, roleErrors) = await _userRepository.AddToRoleAsync(user, AppRoles.Author);
        if (!roleSucceeded)
        {
            throw new IdentityOperationException("Assigning role failed", roleErrors);
        }

        var roles = new List<string> { AppRoles.Author };

        // 6. Generate JWT access token
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        // 7. Generate refresh token
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = RefreshToken.Create(user.Id, rawRefreshToken, ipAddress: null, daysToLive: 7);

        // 8. Persist refresh token hash
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // 9. Return AuthResponse
        var userDto = new UserDto(
            user.Id,
            user.Email ?? request.Email,
            user.UserName ?? request.UserName,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            roles
        );

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(15),
            userDto
        );
    }
}
