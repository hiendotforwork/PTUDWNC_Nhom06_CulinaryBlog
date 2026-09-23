using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
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
        builder.HasPostgresExtension("unaccent");
        // Separate from Supabase auth/storage/public schemas; access through .NET only.
        builder.HasDefaultSchema("culinary");
        // Limit this context to recipe mappings; assembly-wide scanning also loads auth mappings.
        builder.ApplyConfiguration(new CategoryConfiguration());
        builder.ApplyConfiguration(new RecipeConfiguration());
        builder.ApplyConfiguration(new IngredientConfiguration());
        builder.ApplyConfiguration(new StepConfiguration());
        builder.ApplyConfiguration(new ImageConfiguration());
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            builder.Entity<Recipe>().Ignore("SearchVector");
        }
        else
        {
            builder.Entity<Recipe>().Property<NpgsqlTsVector>("SearchVector").HasColumnType("tsvector");
            builder.Entity<Recipe>().HasIndex("SearchVector").HasDatabaseName("IX_Recipe_SearchVector").HasMethod("GIN");
        }
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
