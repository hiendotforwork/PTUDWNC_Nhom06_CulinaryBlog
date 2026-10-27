namespace CulinaryBlog.Application.Queries.Auth;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto?>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.UserName))
        {
            throw new InvalidOperationException($"User '{user.Id}' is missing a valid Email or UserName.");
        }

        var roles = await _userRepository.GetRolesAsync(user);

        return new UserProfileDto(
            Id: user.Id,
            DisplayName: user.DisplayName,
            Email: user.Email,
            UserName: user.UserName,
            AvatarUrl: user.AvatarUrl,
            Bio: user.Bio,
            Roles: roles?.ToArray() ?? Array.Empty<string>(),
            EmailConfirmed: user.EmailConfirmed,
            CreatedAt: user.CreatedAt
        );
    }
}
