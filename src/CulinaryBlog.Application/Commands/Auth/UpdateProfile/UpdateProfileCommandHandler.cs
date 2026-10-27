namespace CulinaryBlog.Application.Commands.Auth.UpdateProfile;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw AuthException.UserNotFound();
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.Bio != null)
        {
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio;
        }

        var (succeeded, errors) = await _userRepository.UpdateAsync(user, cancellationToken);
        if (!succeeded)
        {
            throw new IdentityOperationException("Updating user profile failed", errors);
        }

        var roles = await _userRepository.GetRolesAsync(user);
        var hasPassword = await _userRepository.HasPasswordAsync(user, cancellationToken);

        return new UserProfileDto(
            Id: user.Id,
            DisplayName: user.DisplayName,
            Email: user.Email ?? string.Empty,
            UserName: user.UserName ?? string.Empty,
            AvatarUrl: user.AvatarUrl,
            Bio: user.Bio,
            Roles: roles?.ToArray() ?? Array.Empty<string>(),
            EmailConfirmed: user.EmailConfirmed,
            HasPassword: hasPassword,
            CreatedAt: user.CreatedAt
        );
    }
}
