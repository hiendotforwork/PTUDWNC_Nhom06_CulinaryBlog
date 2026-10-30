namespace CulinaryBlog.Application.Interfaces;

using CulinaryBlog.Domain.Entities;

public record SignInResult(
    bool Succeeded,
    bool IsLockedOut = false,
    bool IsNotAllowed = false,
    DateTime? LockoutEnd = null
);

public record UserOperationError(string Code, string Description);
public record UserOperationResult(bool Succeeded, IReadOnlyList<UserOperationError> Errors);

public interface IUserRepository
{
    Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(ApplicationUser user, string role);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddLoginAsync(ApplicationUser user, string provider, string providerKey, string? displayName, string? avatarUrl);
    Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<bool> VerifyEmailConfirmationTokenAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, IEnumerable<string> Errors)> ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default);
    Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<UserOperationResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<UserOperationResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken = default);
}
