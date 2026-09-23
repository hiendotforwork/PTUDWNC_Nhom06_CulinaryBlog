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
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
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

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Check email uniqueness (early check)
        var existingEmailUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingEmailUser != null)
        {
            throw AuthConflictException.EmailExists();
        }

        // 2. Check username uniqueness (early check)
        var existingNameUser = await _userRepository.FindByUserNameAsync(request.UserName, cancellationToken);
        if (existingNameUser != null)
        {
            throw AuthConflictException.UserNameExists();
        }

        // 3. Begin atomic transaction for user creation, role assignment, and token persistence
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // 4. Create user entity
        var user = ApplicationUser.Create(request.DisplayName, request.Email, request.UserName);

        // 5. Save user with hashed password
        var (createSucceeded, createErrors) = await _userRepository.CreateAsync(user, request.Password);
        if (!createSucceeded)
        {
            var errorsList = createErrors.ToList();

            // Detect concurrent race-condition duplicates from UserManager / DB constraints
            if (errorsList.Any(e => e.Contains("DuplicateEmail", StringComparison.OrdinalIgnoreCase) ||
                                   (e.Contains("email", StringComparison.OrdinalIgnoreCase) && 
                                    (e.Contains("already", StringComparison.OrdinalIgnoreCase) || e.Contains("taken", StringComparison.OrdinalIgnoreCase)))))
            {
                throw AuthConflictException.EmailExists();
            }

            if (errorsList.Any(e => e.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase) ||
                                   (e.Contains("user name", StringComparison.OrdinalIgnoreCase) && 
                                    (e.Contains("already", StringComparison.OrdinalIgnoreCase) || e.Contains("taken", StringComparison.OrdinalIgnoreCase)))))
            {
                throw AuthConflictException.UserNameExists();
            }

            throw new IdentityOperationException("User creation failed", errorsList);
        }

        // 6. Assign default Author role
        var (roleSucceeded, roleErrors) = await _userRepository.AddToRoleAsync(user, AppRoles.Author);
        if (!roleSucceeded)
        {
            throw new IdentityOperationException("Assigning role failed", roleErrors);
        }

        var roles = new List<string> { AppRoles.Author };

        // 7. Generate JWT access token
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        // 8. Generate refresh token
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = RefreshToken.Create(user.Id, rawRefreshToken, ipAddress: null, daysToLive: 7);

        // 9. Persist refresh token hash
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // 10. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        // 11. Return AuthResponse
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
