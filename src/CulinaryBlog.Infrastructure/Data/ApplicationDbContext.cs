// Tệp này cấu hình DbContext hợp nhất Identity và dữ liệu Recipe trong PostgreSQL/Supabase.
// Chức năng: cung cấp DbSet, áp dụng mapping (OnModelCreating), audit/RowVersion (AuditRecipeEntities)
// và chặn SaveChanges để tự cập nhật thông tin theo dõi.

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

// Class làm cổng truy cập database cho xác thực và module công thức.
// Input: DbContextOptions. Output: DbContext có các DbSet và cấu hình schema tương ứng.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    // Chức năng: khởi tạo DbContext bằng cấu hình kết nối được DI cung cấp.
    // Input: options. Output: instance ApplicationDbContext.
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

    // Chức năng: áp dụng cấu hình Identity, Recipe và chuyển Identity sang schema public.
    // Input: ModelBuilder. Output: mô hình EF Core đã cấu hình.
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

    // Chức năng: gán UpdatedAt và RowVersion mới cho thực thể Recipe thay đổi.
    // Input: các entity đang được ChangeTracker theo dõi. Output: cập nhật giá trị audit trong bộ nhớ.
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

    // Chức năng: audit rồi lưu đồng bộ các thay đổi.
    // Input: acceptAllChangesOnSuccess. Output: số bản ghi bị tác động.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AuditRecipeEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    // Chức năng: audit rồi lưu bất đồng bộ các thay đổi.
    // Input: acceptAllChangesOnSuccess và cancellationToken. Output: Task chứa số bản ghi bị tác động.
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AuditRecipeEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
