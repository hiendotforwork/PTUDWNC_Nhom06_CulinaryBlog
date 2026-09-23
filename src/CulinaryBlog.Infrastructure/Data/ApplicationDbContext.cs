namespace CulinaryBlog.Infrastructure.Data;

using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data.Configurations;
using RecipeCategoryConfiguration = CulinaryBlog.Infrastructure.Persistence.CategoryConfiguration;
using RecipeEntityConfiguration = CulinaryBlog.Infrastructure.Persistence.RecipeConfiguration;
using RecipeIngredientConfiguration = CulinaryBlog.Infrastructure.Persistence.IngredientConfiguration;
using RecipeStepConfiguration = CulinaryBlog.Infrastructure.Persistence.StepConfiguration;
using RecipeImageConfiguration = CulinaryBlog.Infrastructure.Persistence.ImageConfiguration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Limit this context to authentication mappings; the assembly also contains recipe mappings.
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        if (Database.IsNpgsql())
        {
            builder.ApplyConfiguration(new RecipeCategoryConfiguration());
            builder.ApplyConfiguration(new RecipeEntityConfiguration());
            builder.ApplyConfiguration(new RecipeIngredientConfiguration());
            builder.ApplyConfiguration(new RecipeStepConfiguration());
            builder.ApplyConfiguration(new RecipeImageConfiguration());
        }
        else
        {
            // Authentication integration tests use EF InMemory, which cannot map NpgsqlTsVector.
            builder.Ignore<Category>();
            builder.Ignore<Recipe>();
            builder.Ignore<RecipeIngredient>();
            builder.Ignore<RecipeStep>();
            builder.Ignore<RecipeImage>();
        }
    }

    private void AuditRecipeEntities()
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
        AuditRecipeEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AuditRecipeEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}