namespace CulinaryBlog.Application.Queries.Auth;

using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using MediatR;

public record GetEmailConfirmationStatusQuery(string UserId, string Token) : IRequest<bool>;

public class GetEmailConfirmationStatusQueryHandler : IRequestHandler<GetEmailConfirmationStatusQuery, bool>
{
    private readonly IUserRepository _userRepository;

    public GetEmailConfirmationStatusQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(GetEmailConfirmationStatusQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw AuthException.InvalidConfirmationToken();
        }

        // Match confirm replay behavior after token keys rotate; this path issues no session.
        if (user.EmailConfirmed)
        {
            return true;
        }

        if (!await _userRepository.VerifyEmailConfirmationTokenAsync(user, request.Token, cancellationToken))
        {
            throw AuthException.InvalidConfirmationToken();
        }

        return false;
    }
}
