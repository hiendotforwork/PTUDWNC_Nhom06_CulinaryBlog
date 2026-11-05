using System.Text.Json;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class OfficialRecipeDataLoaderTests
{
    [Fact]
    public async Task MapsRecipeFieldsAndDifficulty()
    {
        using var files = new DatasetFiles();
        files.WriteImage("001.jpg");

        var dataset = await OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath);

        var recipe = Assert.Single(dataset.Recipes);
        Assert.Equal(1, recipe.SourceId);
        Assert.Equal("Thịt kho tàu", recipe.Name);
        Assert.Equal("Món mặn", recipe.Category);
        Assert.Equal(20, recipe.PrepTimeMinutes);
        Assert.Equal(60, recipe.CookTimeMinutes);
        Assert.Equal(4, recipe.Servings);
        Assert.Equal(RecipeDifficulty.Easy, recipe.Difficulty);
        Assert.Equal(["Thịt ba chỉ: 500g"], recipe.Ingredients);
        Assert.Equal(["Kho thịt đến mềm."], recipe.Steps);
        Assert.Equal(Path.Combine(files.DirectoryPath, "images", "001.jpg"), recipe.ImagePath);
    }

    [Fact]
    public async Task RejectsMissingSourceFiles()
    {
        using var files = new DatasetFiles(writeRecipes: false);

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsMalformedRecipeJson()
    {
        using var files = new DatasetFiles();
        File.WriteAllText(Path.Combine(files.DirectoryPath, "recipes.json"), "{");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsMalformedManifestJson()
    {
        using var files = new DatasetFiles();
        File.WriteAllText(Path.Combine(files.DirectoryPath, "images", "manifest.json"), "[");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsMalformedManifestEntry()
    {
        using var files = new DatasetFiles();
        File.WriteAllText(Path.Combine(files.DirectoryPath, "images", "manifest.json"), "{\"1\":{\"file\":42}}");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsManifestEntryMissingRequiredMetadata()
    {
        using var files = new DatasetFiles();
        files.WriteImage("001.jpg");
        File.WriteAllText(Path.Combine(files.DirectoryPath, "images", "manifest.json"), "{\"1\":{\"file\":\"001.jpg\"}}");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsMalformedUnusedManifestEntry()
    {
        using var files = new DatasetFiles();
        files.WriteImage("001.jpg");
        File.WriteAllText(Path.Combine(files.DirectoryPath, "images", "manifest.json"),
            "{\"1\":{\"name\":\"Thịt kho tàu\",\"file\":\"001.jpg\",\"bytes\":1},\"99\":{\"file\":\"missing.jpg\"}}");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsManifestPathOutsideImagesDirectory()
    {
        using var files = new DatasetFiles();
        files.WriteImage("outside.jpg", inImagesDirectory: false);
        files.WriteManifest("../../outside.jpg");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Theory]
    [InlineData("Trung bình", RecipeDifficulty.Medium)]
    [InlineData("Khó", RecipeDifficulty.Hard)]
    public async Task MapsVietnameseDifficulty(string source, RecipeDifficulty expected)
    {
        using var files = new DatasetFiles(difficulty: source);
        files.WriteImage("001.jpg");

        var dataset = await OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath);

        Assert.Equal(expected, Assert.Single(dataset.Recipes).Difficulty);
    }

    [Fact]
    public async Task RejectsUnknownDifficulty()
    {
        using var files = new DatasetFiles(difficulty: "Rất khó");
        files.WriteImage("001.jpg");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsMissingRecipeImageMapping()
    {
        using var files = new DatasetFiles();
        files.WriteManifest("001.jpg", recipeId: "2");
        files.WriteImage("001.jpg");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    [Fact]
    public async Task RejectsTextLongerThanEntityLimit()
    {
        using var files = new DatasetFiles(name: new string('a', 201));
        files.WriteImage("001.jpg");

        await Assert.ThrowsAsync<InvalidDataException>(() => OfficialRecipeDataLoader.LoadAsync(files.DirectoryPath));
    }

    private sealed class DatasetFiles : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public DatasetFiles(string name = "Thịt kho tàu", string difficulty = "Dễ", bool writeRecipes = true)
        {
            Directory.CreateDirectory(Path.Combine(DirectoryPath, "images"));
            if (writeRecipes)
                File.WriteAllText(Path.Combine(DirectoryPath, "recipes.json"), JsonSerializer.Serialize(new
                {
                    recipes = new[]
                    {
                        new
                        {
                            id = 1,
                            name,
                            category = "Món mặn",
                            ingredients = new[] { "Thịt ba chỉ: 500g" },
                            steps = new[] { "Kho thịt đến mềm." },
                            prep_time_minutes = 20,
                            cook_time_minutes = 60,
                            difficulty,
                            servings = 4
                        }
                    }
                }));
            WriteManifest("001.jpg");
        }

        public void WriteManifest(string file, string recipeId = "1") =>
            File.WriteAllText(Path.Combine(DirectoryPath, "images", "manifest.json"), JsonSerializer.Serialize(
                new Dictionary<string, object> { [recipeId] = new { name = "Thịt kho tàu", file, bytes = 1 } }));

        public void WriteImage(string file, bool inImagesDirectory = true) =>
            File.WriteAllBytes(Path.Combine(inImagesDirectory ? Path.Combine(DirectoryPath, "images") : DirectoryPath, file), [1]);

        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }
}
