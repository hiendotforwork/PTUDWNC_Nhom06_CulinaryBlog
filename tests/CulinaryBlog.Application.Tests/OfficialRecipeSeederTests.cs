using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class OfficialRecipeSeederTests
{
    [Fact]
    public async Task SyncImportsPublishedRecipeWithOrderedChildrenAndPrimaryImage()
    {
        using var files = new RecipeFiles();
        await using var db = CreateContext();
        var storage = new RecordingStorage();

        var result = await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, Recipe(1)));

        var recipe = await db.Recipes.Include(x => x.Category).Include(x => x.Ingredients)
            .Include(x => x.Steps).Include(x => x.Images).SingleAsync();
        Assert.Equal(1, result.Recipes);
        Assert.Equal(1, result.Categories);
        Assert.Equal(1, result.Ingredients);
        Assert.Equal(2, result.Steps);
        Assert.Equal(1, result.Images);
        Assert.Equal("Thịt kho tàu", recipe.Title);
        Assert.Equal("official-recipe-1-thit-kho-tau", recipe.Slug);
        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.NotNull(recipe.PublishedAt);
        Assert.Equal("Món mặn", recipe.Category.Name);
        Assert.Equal("Thịt ba chỉ: 500g", Assert.Single(recipe.Ingredients).Name);
        Assert.Equal(new[] { "Bước 1", "Bước 2" }, recipe.Steps.OrderBy(x => x.StepNumber).Select(x => x.Title));
        Assert.Equal("Kho thịt đến mềm.\nNêm nếm vừa ăn.", recipe.Instructions);
        Assert.Equal(storage.UploadedUrls.Single(), Assert.Single(recipe.Images).OriginalUrl);
        Assert.True(recipe.Images.Single().IsPrimary);
    }

    [Fact]
    public async Task SyncUsesImageSignatureWhenFileExtensionIsWrong()
    {
        using var files = new RecipeFiles();
        File.WriteAllBytes(files.ImagePath, [137, 80, 78, 71, 13, 10, 26, 10]);
        await using var db = CreateContext();
        var storage = new RecordingStorage();

        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, Recipe(1)));

        Assert.Equal(("image/png", ".png"), Assert.Single(storage.UploadedTypes));
    }

    [Fact]
    public async Task SyncRejectsInvalidImageBeforeUploadingAnyRecipe()
    {
        using var validImage = new RecipeFiles();
        using var invalidImage = new RecipeFiles();
        File.WriteAllBytes(invalidImage.ImagePath, [1, 2, 3]);
        await using var db = CreateContext();
        var storage = new RecordingStorage();
        var dataset = new OfficialRecipeDataset([
            Recipe(1) with { ImagePath = validImage.ImagePath },
            Recipe(2, "Cá kho tộ", "Mì") with { ImagePath = invalidImage.ImagePath }
        ]);

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeSeeder.SyncAsync(db, storage, dataset));

        Assert.Empty(storage.UploadedUrls);
        Assert.Empty(await db.Recipes.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task SyncIsIdempotentAndPreservesPublishedAt()
    {
        using var files = new RecipeFiles();
        await using var db = CreateContext();
        var storage = new RecordingStorage();
        var dataset = Dataset(files, Recipe(1));

        await OfficialRecipeSeeder.SyncAsync(db, storage, dataset);
        var original = await db.Recipes.SingleAsync();
        var id = original.Id;
        var publishedAt = original.PublishedAt;
        var childIds = await db.RecipeSteps.Select(x => x.Id).OrderBy(x => x).ToArrayAsync();

        await OfficialRecipeSeeder.SyncAsync(db, storage, dataset);

        Assert.Equal(1, await db.Recipes.CountAsync());
        Assert.Equal(1, await db.Categories.CountAsync());
        Assert.Equal(1, await db.RecipeImages.CountAsync());
        Assert.Equal(2, await db.RecipeSteps.CountAsync());
        Assert.Equal(id, (await db.Recipes.SingleAsync()).Id);
        Assert.Equal(publishedAt, (await db.Recipes.SingleAsync()).PublishedAt);
        Assert.Equal(childIds, await db.RecipeSteps.Select(x => x.Id).OrderBy(x => x).ToArrayAsync());
        Assert.Contains(storage.UploadedUrls[0], storage.DeletedUrls);
    }

    [Fact]
    public async Task SyncSoftDeletesRemovedRecordsAndReactivatesThemWhenTheyReturn()
    {
        using var files = new RecipeFiles();
        await using var db = CreateContext();
        var storage = new RecordingStorage();
        var first = Recipe(1);
        var removed = Recipe(2, "Cá kho tộ", "Mì");
        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, first, removed));
        var removedRecipeId = (await db.Recipes.IgnoreQueryFilters().SingleAsync(x => x.Title == "Cá kho tộ")).Id;
        var removedCategoryId = (await db.Categories.IgnoreQueryFilters().SingleAsync(x => x.Name == "Mì")).Id;
        var removedStepIds = await db.RecipeSteps.IgnoreQueryFilters().Where(x => x.RecipeId == removedRecipeId)
            .Select(x => x.Id).ToArrayAsync();

        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, first));

        Assert.True((await db.Recipes.IgnoreQueryFilters().SingleAsync(x => x.Id == removedRecipeId)).IsDeleted);
        Assert.All(await db.RecipeSteps.IgnoreQueryFilters().Where(x => removedStepIds.Contains(x.Id)).ToListAsync(), x => Assert.True(x.IsDeleted));
        Assert.True((await db.Categories.IgnoreQueryFilters().SingleAsync(x => x.Id == removedCategoryId)).IsDeleted);

        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, first, removed));

        Assert.False((await db.Recipes.IgnoreQueryFilters().SingleAsync(x => x.Id == removedRecipeId)).IsDeleted);
        Assert.False((await db.Categories.IgnoreQueryFilters().SingleAsync(x => x.Id == removedCategoryId)).IsDeleted);
    }

    [Fact]
    public async Task SyncReusesAndPreservesCategoryUsedByUnrelatedRecipe()
    {
        using var files = new RecipeFiles();
        await using var db = CreateContext();
        var existingCategory = new Category { Name = "Món mặn", Slug = "mon-man", OrderIndex = 1 };
        var unrelatedRecipe = new Recipe
        {
            Title = "Món của thành viên", Slug = "member-recipe", Description = "Công thức riêng của thành viên.",
            PrepTime = 10, CookTime = 5, Servings = 2, Difficulty = RecipeDifficulty.Easy,
            Status = RecipeStatus.Draft, Category = existingCategory, AuthorId = "member"
        };
        db.Add(unrelatedRecipe);
        await db.SaveChangesAsync();
        var categoryId = existingCategory.Id;
        var storage = new RecordingStorage();

        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, Recipe(1)));

        Assert.Equal(categoryId, (await db.Recipes.SingleAsync(x => x.AuthorId == OfficialRecipeSeeder.SeedAuthorId)).CategoryId);
        await OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, Recipe(1, category: "Bún")));

        Assert.False((await db.Categories.IgnoreQueryFilters().SingleAsync(x => x.Id == categoryId)).IsDeleted);
        Assert.False(unrelatedRecipe.IsDeleted);
    }

    [Fact]
    public async Task UploadFailureDeletesEarlierUploadsWithoutChangingDatabase()
    {
        using var files = new RecipeFiles();
        await using var db = CreateContext();
        var storage = new RecordingStorage { FailOnUploadNumber = 2 };

        await Assert.ThrowsAsync<IOException>(() => OfficialRecipeSeeder.SyncAsync(db, storage,
            Dataset(files, Recipe(1), Recipe(2, "Cá kho tộ", "Mì"))));

        Assert.Empty(await db.Recipes.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await db.Categories.IgnoreQueryFilters().ToListAsync());
        Assert.Equal(storage.UploadedUrls, storage.DeletedUrls);
    }

    [Fact]
    public async Task DatabaseSaveFailureDeletesNewUploadsAndKeepsExistingData()
    {
        using var files = new RecipeFiles();
        await using var db = CreateFailingContext();
        var category = new Category { Name = "Món riêng", Slug = "mon-rieng", OrderIndex = 1 };
        var unrelated = new Recipe
        {
            Title = "Món thành viên", Slug = "member-recipe", Description = "Công thức riêng.",
            PrepTime = 5, CookTime = 5, Servings = 1, Difficulty = RecipeDifficulty.Easy,
            Status = RecipeStatus.Draft, Category = category, AuthorId = "member"
        };
        db.Add(unrelated);
        await db.SaveChangesAsync();
        db.FailNextSave = true;
        var storage = new RecordingStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() => OfficialRecipeSeeder.SyncAsync(db, storage, Dataset(files, Recipe(1))));

        Assert.Equal("member-recipe", (await db.Recipes.SingleAsync()).Slug);
        Assert.Equal(storage.UploadedUrls, storage.DeletedUrls);
    }

    [Fact]
    public async Task VerifyRejectsMissingOfficialDataset()
    {
        await using var db = CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => OfficialRecipeSeeder.VerifyAsync(db));
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static FailingSaveContext CreateFailingContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static OfficialRecipeDataset Dataset(RecipeFiles files, params OfficialRecipeData[] recipes) =>
        new(recipes.Select(x => x with { ImagePath = files.ImagePath }).ToArray());

    private static OfficialRecipeData Recipe(int id, string name = "Thịt kho tàu", string category = "Món mặn") => new(
        id, name, category, 20, 60, 4, RecipeDifficulty.Easy,
        ["Thịt ba chỉ: 500g"], ["Kho thịt đến mềm.", "Nêm nếm vừa ăn."], "");

    private sealed class RecipeFiles : IDisposable
    {
        public string ImagePath { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jpg");
        public RecipeFiles() => File.WriteAllBytes(ImagePath, [255, 216, 255]);
        public void Dispose() => File.Delete(ImagePath);
    }

    private sealed class RecordingStorage : IFileStorageService
    {
        public List<string> UploadedUrls { get; } = [];
        public List<string> DeletedUrls { get; } = [];
        public List<(string ContentType, string Extension)> UploadedTypes { get; } = [];
        public int? FailOnUploadNumber { get; init; }

        public async Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken)
        {
            await stream.CopyToAsync(Stream.Null, cancellationToken);
            UploadedTypes.Add((contentType, extension));
            if (FailOnUploadNumber == UploadedUrls.Count + 1)
                throw new IOException("simulated upload failure");
            var url = $"https://minio.local/recipes/{Guid.NewGuid():N}.jpg";
            UploadedUrls.Add(url);
            return new StoredFile(url);
        }

        public Task DeleteAsync(string url, CancellationToken cancellationToken)
        {
            DeletedUrls.Add(url);
            return Task.CompletedTask;
        }

        public bool IsOwnedUrl(string url) => url.StartsWith("https://minio.local/", StringComparison.Ordinal);
    }

    private sealed class FailingSaveContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("simulated database failure");
            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
