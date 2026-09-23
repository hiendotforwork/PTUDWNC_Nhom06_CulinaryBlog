namespace CulinaryBlog.Application.Interfaces;

using CulinaryBlog.Domain.Entities;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
