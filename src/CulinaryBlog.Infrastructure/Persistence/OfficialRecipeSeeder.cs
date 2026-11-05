using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed record OfficialRecipeSyncResult(int Categories, int Recipes, int Ingredients, int Steps, int Images);

public static class OfficialRecipeSeeder
{
    public const string SeedAuthorId = "official-recipe-dataset";
    private const string LegacySlugPrefix = "lab2-chuong-";
    private const string StorageFolder = "recipes/official-recipe-dataset";

    public static async Task<OfficialRecipeSyncResult> SyncAsync(
        ApplicationDbContext db,
        IFileStorageService storage,
        OfficialRecipeDataset dataset,
        CancellationToken cancellationToken = default)
    {
        if (dataset.Recipes.Count == 0)
            throw new InvalidDataException("The official recipe dataset is empty.");

        var uploadedUrls = new List<string>();
        var newImageUrls = new Dictionary<int, string>();
        var oldImageUrls = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var recipe in dataset.Recipes)
            {
                var extension = Path.GetExtension(recipe.ImagePath).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".avif" => "image/avif",
                    _ => throw new InvalidDataException($"Unsupported image extension for recipe {recipe.SourceId}.")
                };
                await using var image = File.OpenRead(recipe.ImagePath);
                var stored = await storage.UploadAsync(image, contentType, extension, StorageFolder, cancellationToken);
                uploadedUrls.Add(stored.Url);
                if (!storage.IsOwnedUrl(stored.Url))
                    throw new InvalidOperationException($"Storage returned a URL it does not own for recipe {recipe.SourceId}.");
                newImageUrls.Add(recipe.SourceId, stored.Url);
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var syncTime = DateTimeOffset.UtcNow;
            var oldRecipes = await db.Recipes.IgnoreQueryFilters()
                .Where(x => x.AuthorId == SeedAuthorId)
                .Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images)
                .ToListAsync(cancellationToken);
            var legacyRecipes = await db.Recipes.IgnoreQueryFilters()
                .Where(x => x.Slug.StartsWith(LegacySlugPrefix))
                .Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images)
                .ToListAsync(cancellationToken);
            foreach (var image in oldRecipes.SelectMany(x => x.Images).Concat(legacyRecipes.SelectMany(x => x.Images)))
                AddObjectUrls(oldImageUrls, image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl);

            await EnsureSeedAuthorAsync(db, cancellationToken);
            var categories = await db.Categories.IgnoreQueryFilters().ToListAsync(cancellationToken);
            var ownedCategoryIds = categories.Where(IsSeedOwnedCategory).Select(x => x.Id).ToHashSet();
            var desiredCategories = new Dictionary<string, Category>(StringComparer.Ordinal);
            var categoryNames = dataset.Recipes.Select(x => x.Category).Distinct(StringComparer.Ordinal).ToArray();
            for (var index = 0; index < categoryNames.Length; index++)
            {
                var name = categoryNames[index];
                var slug = Slugify(name);
                var stableId = Key("category:" + slug);
                var category = categories.FirstOrDefault(x => x.Slug == slug);
                if (category is null)
                {
                    category = new Category { Id = stableId, Name = name, Slug = slug, OrderIndex = index };
                    db.Categories.Add(category);
                    categories.Add(category);
                    ownedCategoryIds.Add(stableId);
                }
                else if (category.Id == stableId)
                {
                    category.Name = name;
                    category.OrderIndex = index;
                    category.IsDeleted = false;
                    category.Description = "Danh mục trong bộ dữ liệu công thức chính thức.";
                    ownedCategoryIds.Add(category.Id);
                }
                else if (category.IsDeleted)
                {
                    throw new InvalidOperationException($"Deleted category slug '{slug}' is owned by unrelated content.");
                }
                desiredCategories.Add(name, category);
            }

            var oldRecipesById = oldRecipes.ToDictionary(x => x.Id);
            var desiredRecipeIds = new HashSet<Guid>();
            foreach (var source in dataset.Recipes)
            {
                var recipeId = Key($"recipe:{source.SourceId}");
                desiredRecipeIds.Add(recipeId);
                oldRecipesById.TryGetValue(recipeId, out var recipe);
                if (recipe is not null && recipe.AuthorId != SeedAuthorId)
                    throw new InvalidOperationException($"Recipe ID {recipeId} is owned by another author.");
                var isNew = recipe is null;
                recipe ??= new Recipe { Id = recipeId, AuthorId = SeedAuthorId, PublishedAt = syncTime };
                recipe.Title = source.Name;
                recipe.Slug = $"official-recipe-{source.SourceId}-{Slugify(source.Name)}";
                recipe.Description = $"Công thức món {source.Name}.";
                recipe.Instructions = string.Join("\n", source.Steps);
                recipe.PrepTime = source.PrepTimeMinutes;
                recipe.CookTime = source.CookTimeMinutes;
                recipe.Servings = source.Servings;
                recipe.Difficulty = source.Difficulty;
                recipe.Status = RecipeStatus.Published;
                recipe.PublishedAt ??= syncTime;
                recipe.Category = desiredCategories[source.Category];
                recipe.CategoryId = recipe.Category.Id;
                recipe.AuthorId = SeedAuthorId;
                recipe.IsDeleted = false;
                recipe.Nutrition = new RecipeNutrition();
                if (isNew)
                    db.Recipes.Add(recipe);

                SyncIngredients(recipe, source);
                SyncSteps(recipe, source);
                SyncImage(recipe, source, newImageUrls[source.SourceId]);
            }

            foreach (var removed in oldRecipes.Where(x => !desiredRecipeIds.Contains(x.Id)))
                SoftDeleteRecipe(removed);
            foreach (var removedLegacy in legacyRecipes)
                SoftDeleteRecipe(removedLegacy);

            var activeOfficialCategories = desiredCategories.Values.Select(x => x.Id).ToHashSet();
            foreach (var category in categories.Where(x => ownedCategoryIds.Contains(x.Id) && !activeOfficialCategories.Contains(x.Id)))
            {
                var usedByOtherContent = await db.Recipes.IgnoreQueryFilters().AnyAsync(x =>
                    !x.IsDeleted && x.AuthorId != SeedAuthorId && !x.Slug.StartsWith(LegacySlugPrefix) && x.CategoryId == category.Id,
                    cancellationToken);
                if (!usedByOtherContent)
                    category.IsDeleted = true;
            }
            foreach (var category in categories.Where(x => x.Slug.StartsWith(LegacySlugPrefix, StringComparison.Ordinal)))
            {
                var usedByOtherContent = await db.Recipes.IgnoreQueryFilters().AnyAsync(x =>
                    !x.IsDeleted && !x.Slug.StartsWith(LegacySlugPrefix) && x.CategoryId == category.Id,
                    cancellationToken);
                if (!usedByOtherContent)
                    category.IsDeleted = true;
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception original)
        {
            var cleanupErrors = await TryDeleteObjectsAsync(db, storage, uploadedUrls, cancellationToken);
            if (cleanupErrors.Count > 0)
                throw new AggregateException("Recipe sync failed and some uploaded objects could not be cleaned up.",
                    new[] { original }.Concat(cleanupErrors));
            throw;
        }

        await DeleteUnreferencedObjectsAsync(db, storage, oldImageUrls, cancellationToken);

        return new OfficialRecipeSyncResult(
            dataset.Recipes.Select(x => x.Category).Distinct(StringComparer.Ordinal).Count(),
            dataset.Recipes.Count,
            dataset.Recipes.Sum(x => x.Ingredients.Count),
            dataset.Recipes.Sum(x => x.Steps.Count),
            dataset.Recipes.Count);
    }

    public static async Task<OfficialRecipeSyncResult> VerifyAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var recipes = await db.Recipes.Where(x => x.AuthorId == SeedAuthorId)
            .Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images)
            .ToListAsync(cancellationToken);
        var categoryCount = recipes.Select(x => x.CategoryId).Distinct().Count();
        if (recipes.Count != 100 || categoryCount != 13 || recipes.Any(x =>
                x.Status != RecipeStatus.Published || x.PublishedAt is null || x.Ingredients.Count == 0 || x.Steps.Count == 0
                || x.Images.Count != 1 || x.Images.Count(image => image.IsPrimary) != 1))
            throw new InvalidOperationException($"Official recipe dataset requirements not met: {recipes.Count} recipes, {categoryCount} categories.");

        return new OfficialRecipeSyncResult(categoryCount, recipes.Count, recipes.Sum(x => x.Ingredients.Count),
            recipes.Sum(x => x.Steps.Count), recipes.Sum(x => x.Images.Count));
    }

    private static async Task EnsureSeedAuthorAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(x => x.Id == SeedAuthorId, cancellationToken))
            db.Users.Add(new CulinaryBlog.Domain.Entities.ApplicationUser
            {
                Id = SeedAuthorId,
                UserName = "official.recipes",
                NormalizedUserName = "OFFICIAL.RECIPES",
                Email = "official-recipes@example.invalid",
                NormalizedEmail = "OFFICIAL-RECIPES@EXAMPLE.INVALID",
                DisplayName = "CulinaryBlog Recipes",
                IsActive = false,
                EmailConfirmed = true
            });
    }

    private static void SyncIngredients(Recipe recipe, OfficialRecipeData source)
    {
        var wanted = new HashSet<Guid>();
        for (var index = 0; index < source.Ingredients.Count; index++)
        {
            var id = Key($"ingredient:{source.SourceId}:{index}");
            wanted.Add(id);
            var ingredient = recipe.Ingredients.FirstOrDefault(x => x.Id == id);
            if (ingredient is null)
            {
                ingredient = new RecipeIngredient { Id = id, RecipeId = recipe.Id, Recipe = recipe };
                recipe.Ingredients.Add(ingredient);
            }
            ingredient.Name = source.Ingredients[index];
            ingredient.OrderIndex = index;
            ingredient.IsDeleted = false;
        }
        foreach (var removed in recipe.Ingredients.Where(x => !wanted.Contains(x.Id)))
            removed.IsDeleted = true;
    }

    private static void SyncSteps(Recipe recipe, OfficialRecipeData source)
    {
        var wanted = new HashSet<Guid>();
        for (var index = 0; index < source.Steps.Count; index++)
        {
            var id = Key($"step:{source.SourceId}:{index}");
            wanted.Add(id);
            var step = recipe.Steps.FirstOrDefault(x => x.Id == id);
            if (step is null)
            {
                step = new RecipeStep { Id = id, RecipeId = recipe.Id, Recipe = recipe };
                recipe.Steps.Add(step);
            }
            step.StepNumber = index + 1;
            step.Title = $"Bước {index + 1}";
            step.Description = source.Steps[index];
            step.IsDeleted = false;
        }
        foreach (var removed in recipe.Steps.Where(x => !wanted.Contains(x.Id)))
            removed.IsDeleted = true;
    }

    private static void SyncImage(Recipe recipe, OfficialRecipeData source, string newUrl)
    {
        var id = Key($"image:{source.SourceId}");
        var image = recipe.Images.FirstOrDefault(x => x.Id == id);
        if (image is null)
        {
            image = new RecipeImage { Id = id, RecipeId = recipe.Id, Recipe = recipe };
            recipe.Images.Add(image);
        }
        image.OriginalUrl = newUrl;
        image.AltText = source.Name;
        image.IsPrimary = true;
        image.OrderIndex = 0;
        image.IsDeleted = false;
        foreach (var extra in recipe.Images.Where(x => x.Id != id))
            extra.IsDeleted = true;
    }

    private static void SoftDeleteRecipe(Recipe recipe)
    {
        recipe.IsDeleted = true;
        foreach (var entity in recipe.Ingredients.Cast<BaseEntity>().Concat(recipe.Steps).Concat(recipe.Images))
            entity.IsDeleted = true;
    }

    private static bool IsSeedOwnedCategory(Category category) => category.Id == Key("category:" + category.Slug);

    private static void AddObjectUrls(HashSet<string> urls, params string?[] candidates)
    {
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            urls.Add(candidate!);
    }

    private static async Task DeleteUnreferencedObjectsAsync(
        ApplicationDbContext db,
        IFileStorageService storage,
        IEnumerable<string> candidates,
        CancellationToken cancellationToken)
    {
        var errors = new List<Exception>();
        foreach (var url in candidates)
        {
            try
            {
                if (!storage.IsOwnedUrl(url) || await IsReferencedAsync(db, url, cancellationToken))
                    continue;
                await storage.DeleteAsync(url, cancellationToken);
            }
            catch (Exception exception) { errors.Add(new IOException($"Could not delete old object {url}.", exception)); }
        }
        if (errors.Count > 0)
            throw new AggregateException("Database sync committed, but some old image objects could not be deleted.", errors);
    }

    private static async Task<bool> IsReferencedAsync(ApplicationDbContext db, string url, CancellationToken cancellationToken) =>
        await db.RecipeImages.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted
            && (x.OriginalUrl == url || x.MediumUrl == url || x.ThumbnailUrl == url), cancellationToken)
        || await db.RecipeSteps.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted && x.ImageUrl == url, cancellationToken)
        || await db.Users.AnyAsync(x => x.AvatarUrl == url, cancellationToken);

    private static async Task<List<Exception>> TryDeleteObjectsAsync(
        ApplicationDbContext db,
        IFileStorageService storage,
        IEnumerable<string> urls,
        CancellationToken cancellationToken)
    {
        var errors = new List<Exception>();
        foreach (var url in urls.Distinct(StringComparer.Ordinal))
        {
            try
            {
                if (await IsReferencedAsync(db, url, cancellationToken))
                    continue;
                await storage.DeleteAsync(url, cancellationToken);
            }
            catch (Exception exception) { errors.Add(exception); }
        }
        return errors;
    }

    private static Guid Key(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes("official-recipe:" + value))[..16]);

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var separatorPending = false;
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            var normalizedCharacter = character == 'đ' ? 'd' : character;
            if (char.IsLetterOrDigit(normalizedCharacter))
            {
                if (separatorPending && builder.Length > 0)
                    builder.Append('-');
                builder.Append(normalizedCharacter);
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
