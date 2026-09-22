namespace CulinaryBlog.Domain.Entities;

public class ApplicationUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsEmailVerified { get; set; }

    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
