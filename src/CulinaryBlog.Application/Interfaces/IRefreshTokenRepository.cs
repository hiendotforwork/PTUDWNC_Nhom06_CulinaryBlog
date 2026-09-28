namespace CulinaryBlog.Application.Interfaces;

using CulinaryBlog.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<int> RevokeByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
