namespace CulinaryBlog.Domain.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<string>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public static ApplicationUser Create(string displayName, string email, string userName)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = displayName,
            Email = email,
            UserName = userName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            LockoutEnabled = true,
            AccessFailedCount = 0
        };
    }
}
