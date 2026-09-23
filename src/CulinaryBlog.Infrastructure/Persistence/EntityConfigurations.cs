using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence;

internal static class Mapping
{
    public static void Base<T>(EntityTypeBuilder<T> b, string table) where T : BaseEntity
    {
        b.ToTable(table, "culinary", t => t.HasCheckConstraint($"CK_{table}_RowVersion", "octet_length(\"RowVersion\") = 16"));
        b.HasKey(x => x.Id);
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.Property(x => x.IsDeleted).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).IsRequired();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        Mapping.Base(b, "Categories");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => x.Slug).IsUnique();
        b.ToTable(t => {
            t.HasCheckConstraint("CK_Categories_Name", "length(btrim(\"Name\")) > 0");
            t.HasCheckConstraint("CK_Categories_Slug", "length(btrim(\"Slug\")) > 0");
            t.HasCheckConstraint("CK_Categories_Order", "\"OrderIndex\" >= 0");
        });
    }
}

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> b)
    {
        Mapping.Base(b, "Recipes");
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.Instructions).IsRequired().HasDefaultValue("");
        b.Property(x => x.AuthorId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Status).HasConversion<short>();
        b.Property(x => x.Difficulty).HasConversion<short>();
        b.Property<NpgsqlTsVector>("SearchVector").HasColumnType("tsvector").ValueGeneratedOnAddOrUpdate();
        b.HasIndex("SearchVector").HasMethod("GIN");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.CreatedAt, x.Id }).IsDescending().HasFilter("NOT \"IsDeleted\" AND \"Status\" = 1");
        b.HasIndex(x => new { x.AuthorId, x.Status, x.CreatedAt, x.Id }).HasFilter("NOT \"IsDeleted\"");
        b.HasOne(x => x.Category).WithMany(x => x.Recipes).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CulinaryBlog.Domain.Entities.ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.OwnsOne(x => x.Nutrition, n => {
            foreach (var name in new[] { "Calories", "Protein", "Carbohydrates", "Fat", "Fiber", "Sodium" })
                n.Property<decimal?>(name).HasColumnName("Nutrition_" + name).HasPrecision(8, 2);
        });
        b.Navigation(x => x.Nutrition).IsRequired();
        b.ToTable(t => {
            t.HasCheckConstraint("CK_Recipes_Title", "length(btrim(\"Title\")) BETWEEN 5 AND 200");
            t.HasCheckConstraint("CK_Recipes_Slug", "length(btrim(\"Slug\")) > 0");
            t.HasCheckConstraint("CK_Recipes_Description", "length(btrim(\"Description\")) BETWEEN 1 AND 2000");
            t.HasCheckConstraint("CK_Recipes_Times", "\"PrepTime\" > 0 AND \"CookTime\" >= 0 AND \"Servings\" > 0");
            t.HasCheckConstraint("CK_Recipes_Enums", "\"Status\" BETWEEN 0 AND 2 AND \"Difficulty\" BETWEEN 1 AND 4");
            t.HasCheckConstraint("CK_Recipes_PublishedAt", "\"Status\" <> 1 OR \"PublishedAt\" IS NOT NULL");
            foreach (var name in new[] { "Calories", "Protein", "Carbohydrates", "Fat", "Fiber", "Sodium" })
                t.HasCheckConstraint("CK_Recipes_Nutrition_" + name, $"\"Nutrition_{name}\" IS NULL OR \"Nutrition_{name}\" >= 0");
        });
    }
}

public sealed class IngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> b)
    {
        Mapping.Base(b, "RecipeIngredients");
        b.HasQueryFilter(x => !x.IsDeleted && !x.Recipe.IsDeleted);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Quantity).HasPrecision(10, 3);
        b.HasOne(x => x.Recipe).WithMany(x => x.Ingredients).HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.RecipeId, x.OrderIndex, x.Id });
        b.ToTable(t => {
            t.HasCheckConstraint("CK_Ingredients_Name", "length(btrim(\"Name\")) BETWEEN 1 AND 200");
            t.HasCheckConstraint("CK_Ingredients_QuantityUnit", "(\"Quantity\" IS NULL AND \"Unit\" IS NULL) OR (\"Quantity\" IS NOT NULL AND \"Quantity\" > 0 AND \"Unit\" IS NOT NULL AND length(btrim(\"Unit\")) > 0)");
            t.HasCheckConstraint("CK_Ingredients_Order", "\"OrderIndex\" >= 0");
        });
    }
}

public sealed class StepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> b)
    {
        Mapping.Base(b, "RecipeSteps");
        b.HasQueryFilter(x => !x.IsDeleted && !x.Recipe.IsDeleted);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasOne(x => x.Recipe).WithMany(x => x.Steps).HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.RecipeId);
        b.HasIndex(x => new { x.RecipeId, x.StepNumber }).IsUnique().HasFilter("NOT \"IsDeleted\"");
        b.ToTable(t => {
            t.HasCheckConstraint("CK_Steps_Number", "\"StepNumber\" > 0");
            t.HasCheckConstraint("CK_Steps_Title", "length(btrim(\"Title\")) > 0");
            t.HasCheckConstraint("CK_Steps_Description", "length(btrim(\"Description\")) BETWEEN 1 AND 2000");
            t.HasCheckConstraint("CK_Steps_Timer", "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");
        });
    }
}

public sealed class ImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> b)
    {
        Mapping.Base(b, "RecipeImages");
        b.HasQueryFilter(x => !x.IsDeleted && !x.Recipe.IsDeleted);
        b.Property(x => x.OriginalUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.MediumUrl).HasMaxLength(500);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.AltText).HasMaxLength(200);
        b.HasOne(x => x.Recipe).WithMany(x => x.Images).HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.RecipeId, x.OrderIndex, x.Id });
        b.HasIndex(x => x.RecipeId).IsUnique().HasFilter("\"IsPrimary\" AND NOT \"IsDeleted\"");
        b.ToTable(t => {
            t.HasCheckConstraint("CK_Images_Original", "length(btrim(\"OriginalUrl\")) > 0");
            t.HasCheckConstraint("CK_Images_Order", "\"OrderIndex\" >= 0");
        });
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(x => x.Id).HasMaxLength(450);
        b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens", t => t.HasCheckConstraint("CK_RefreshTokens_Expiry", "\"ExpiresAt\" > \"CreatedAt\""));
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        b.Property(x => x.CreatedByIp).HasMaxLength(45);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.ExpiresAt);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
