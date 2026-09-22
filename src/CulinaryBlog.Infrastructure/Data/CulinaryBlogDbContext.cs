using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Data;

public class CulinaryBlogDbContext : DbContext
{
    public CulinaryBlogDbContext(DbContextOptions<CulinaryBlogDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.UserName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.ChildCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.FeaturedImageUrl).HasMaxLength(500);

            entity.HasIndex(x => x.Slug).IsUnique();

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Author)
                .WithMany(x => x.Recipes)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Quantity).HasMaxLength(100);
            entity.Property(x => x.Unit).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Ingredients)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);

            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
            entity.Property(x => x.AltText).HasMaxLength(255);

            entity.HasOne(x => x.Recipe)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
