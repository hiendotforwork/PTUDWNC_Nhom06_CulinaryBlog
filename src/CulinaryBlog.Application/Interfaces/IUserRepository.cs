namespace CulinaryBlog.Application.Interfaces;

using CulinaryBlog.Domain.Entities;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(ApplicationUser user, string role);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
}
