namespace CulinaryBlog.Infrastructure.Services;

using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;

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
}
