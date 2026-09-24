namespace CulinaryBlog.Infrastructure.Services;

using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using SignInResult = CulinaryBlog.Application.Interfaces.SignInResult;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole>? _roleManager;

    public UserRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole>? roleManager = null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _userManager.FindByEmailAsync(email.Trim());
    }

    public async Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        return await _userManager.FindByNameAsync(userName.Trim());
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(ApplicationUser user, string role)
    {
        if (_roleManager != null && !await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    private static readonly string DummyHash = new PasswordHasher<ApplicationUser>().HashPassword(null!, "DummyPassword123!");

    public async Task<SignInResult> CheckPasswordSignInAsync(
        ApplicationUser user,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (user.Email == "security-dummy@culinaryblog.vn")
        {
            _userManager.PasswordHasher.VerifyHashedPassword(user, DummyHash, password);
            return new SignInResult(false);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            return new SignInResult(
                Succeeded: false,
                IsLockedOut: true,
                IsNotAllowed: false,
                LockoutEnd: lockoutEnd?.UtcDateTime);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);
            var lockoutEnd = isLocked ? await _userManager.GetLockoutEndDateAsync(user) : null;

            return new SignInResult(
                Succeeded: false,
                IsLockedOut: isLocked,
                IsNotAllowed: false,
                LockoutEnd: lockoutEnd?.UtcDateTime);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return new SignInResult(Succeeded: true);
    }
}
