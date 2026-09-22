using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CulinaryBlogDbContext(DbContextOptions<CulinaryBlogDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(category => category.Slug).IsUnique();
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(recipe => recipe.Id);
            entity.Property(recipe => recipe.Title).HasMaxLength(300).IsRequired();
            entity.Property(recipe => recipe.Slug).HasMaxLength(300).IsRequired();
            entity.Property(recipe => recipe.Description).HasMaxLength(2000).IsRequired();
            entity.Property(recipe => recipe.Difficulty).HasConversion<short>().IsRequired();
            entity.Property(recipe => recipe.Status).HasConversion<short>().IsRequired();
            entity.HasIndex(recipe => recipe.Status);
            entity.HasIndex(recipe => recipe.CategoryId);
            entity.HasIndex(recipe => recipe.CreatedAt);
            entity.Property<NpgsqlTsVector>("SearchVector")
                .HasColumnType("tsvector");
            entity.HasIndex("SearchVector")
                .HasDatabaseName("IX_Recipe_SearchVector")
                .HasMethod("GIN");
            entity.HasOne(recipe => recipe.Category)
                .WithMany(category => category.Recipes)
                .HasForeignKey(recipe => recipe.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
