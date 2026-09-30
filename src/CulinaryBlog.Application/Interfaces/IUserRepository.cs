namespace CulinaryBlog.Application.Interfaces;

using CulinaryBlog.Domain.Entities;

public record SignInResult(
    bool Succeeded,
    bool IsLockedOut = false,
    bool IsNotAllowed = false,
    DateTime? LockoutEnd = null
);

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(ApplicationUser user, string role);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
}
