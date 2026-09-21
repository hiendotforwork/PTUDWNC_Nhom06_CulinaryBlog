using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CulinaryBlogDbContext(DbContextOptions<CulinaryBlogDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Separate from Supabase auth/storage/public schemas; access through .NET only.
        builder.HasDefaultSchema("culinary");
        builder.ApplyConfigurationsFromAssembly(typeof(CulinaryBlogDbContext).Assembly);
    }

    private void Audit()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Use explicit IsDeleted changes; hard deletion is not enabled.");
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var now = DateTimeOffset.UtcNow;
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            else entry.Entity.UpdatedAt = now;
            entry.Entity.RowVersion = RandomNumberGenerator.GetBytes(16);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Audit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        Audit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
