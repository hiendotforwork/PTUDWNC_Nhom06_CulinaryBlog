using System.Text.Json;
using System.Text.Json.Serialization;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed record OfficialRecipeDataset(IReadOnlyList<OfficialRecipeData> Recipes);

public sealed record OfficialRecipeData(
    int SourceId,
    string Name,
    string Category,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Steps,
    string ImagePath);

public static class OfficialRecipeDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<OfficialRecipeDataset> LoadAsync(string dataDirectory, CancellationToken cancellationToken = default)
    {
        var recipeFile = Path.Combine(dataDirectory, "recipes.json");
        var imagesDirectory = Path.GetFullPath(Path.Combine(dataDirectory, "images"));
        var manifestFile = Path.Combine(imagesDirectory, "manifest.json");
        if (!File.Exists(recipeFile) || !File.Exists(manifestFile))
            throw new InvalidDataException("recipes.json and images/manifest.json are required.");

        try
        {
            await using var recipeStream = File.OpenRead(recipeFile);
            await using var manifestStream = File.OpenRead(manifestFile);
            var recipeRoot = await JsonSerializer.DeserializeAsync<RecipeRoot>(
                recipeStream, JsonOptions, cancellationToken);
            var manifest = await JsonSerializer.DeserializeAsync<Dictionary<string, ManifestEntry>>(
                manifestStream, JsonOptions, cancellationToken);
            if (recipeRoot?.Recipes is not { Count: > 0 } sourceRecipes || manifest is null)
                throw new InvalidDataException("Recipe JSON and image manifest must contain records.");

            var canonicalImagesDirectory = ResolveDirectory(imagesDirectory);
            var validatedImages = new Dictionary<int, (string Name, string Path)>();
            foreach (var (key, image) in manifest)
            {
                if (!int.TryParse(key, out var recipeId) || recipeId <= 0 || key != recipeId.ToString())
                    throw new InvalidDataException($"Image manifest key '{key}' is not a recipe ID.");
                if (image is null)
                    throw new InvalidDataException($"Image manifest entry for recipe {recipeId} is empty.");
                var imageName = RequiredText(image.Name, "image name", 5, 200);
                var imageFile = RequiredText(image.File, "image file", 1, 500);
                if (image.Bytes is not > 0)
                    throw new InvalidDataException($"Image manifest entry for recipe {recipeId} has an invalid byte count.");
                var imagePath = ResolveImagePath(imagesDirectory, canonicalImagesDirectory, imageFile, recipeId);
                if (new FileInfo(imagePath).Length != image.Bytes)
                    throw new InvalidDataException($"Image file size for recipe {recipeId} does not match the manifest.");
                validatedImages.Add(recipeId, (imageName, imagePath));
            }

            var recipeIds = new HashSet<int>();
            var recipes = new List<OfficialRecipeData>(sourceRecipes.Count);
            foreach (var source in sourceRecipes)
            {
                if (source is null)
                    throw new InvalidDataException("Recipe records cannot be null.");
                if (source.Id is not > 0 || !recipeIds.Add(source.Id.Value))
                    throw new InvalidDataException("Recipe IDs must be unique positive integers.");
                var name = RequiredText(source.Name, "recipe name", 5, 200);
                var category = RequiredText(source.Category, "category", 1, 100);
                if (source.PrepTimeMinutes is not > 0 || source.CookTimeMinutes is not >= 0 || source.Servings is not > 0)
                    throw new InvalidDataException($"Recipe {source.Id} has invalid time or serving values.");
                var difficulty = source.Difficulty switch
                {
                    "Dễ" => RecipeDifficulty.Easy,
                    "Trung bình" => RecipeDifficulty.Medium,
                    "Khó" => RecipeDifficulty.Hard,
                    _ => throw new InvalidDataException($"Recipe {source.Id} has unknown difficulty '{source.Difficulty}'.")
                };
                var ingredients = RequiredTexts(source.Ingredients, "ingredients", 200, source.Id.Value);
                var steps = RequiredTexts(source.Steps, "steps", 2000, source.Id.Value);
                if (steps.Count > 0 && $"Bước {steps.Count}".Length > 200)
                    throw new InvalidDataException($"Recipe {source.Id} has too many steps.");

                if (!validatedImages.TryGetValue(source.Id.Value, out var image))
                    throw new InvalidDataException($"Recipe {source.Id} has no image mapping.");
                if (image.Name != name)
                    throw new InvalidDataException($"Image manifest entry for recipe {source.Id} is invalid.");

                recipes.Add(new OfficialRecipeData(source.Id.Value, name, category, source.PrepTimeMinutes.Value,
                    source.CookTimeMinutes.Value, source.Servings.Value, difficulty, ingredients, steps, image.Path));
            }

            return new OfficialRecipeDataset(recipes);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Recipe JSON or image manifest is malformed.", exception);
        }
    }

    private static string RequiredText(string? value, string field, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minLength || value.Length > maxLength)
            throw new InvalidDataException($"{field} must contain {minLength} to {maxLength} characters.");
        return value.Trim();
    }

    private static IReadOnlyList<string> RequiredTexts(List<string?>? values, string field, int maxLength, int recipeId)
    {
        if (values is not { Count: > 0 })
            throw new InvalidDataException($"Recipe {recipeId} must contain {field}.");
        return values.Select((value, index) => RequiredText(value, $"Recipe {recipeId} {field}[{index}]", 1, maxLength)).ToArray();
    }

    private static string ResolveDirectory(string directory)
    {
        var info = new DirectoryInfo(directory);
        return Path.GetFullPath(info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? info.FullName);
    }

    private static string ResolveImagePath(string imagesDirectory, string canonicalImagesDirectory, string file, int recipeId)
    {
        if (Path.IsPathRooted(file))
            throw new InvalidDataException($"Image path for recipe {recipeId} must be relative to images/.");
        var imagePath = Path.GetFullPath(Path.Combine(imagesDirectory, file));
        var relativePath = Path.GetRelativePath(imagesDirectory, imagePath);
        if (!IsWithin(imagesDirectory, imagePath))
            throw new InvalidDataException($"Image path for recipe {recipeId} escapes images/.");
        var canonicalImagePath = Path.GetFullPath(Path.Combine(canonicalImagesDirectory, relativePath));
        if (!IsWithin(canonicalImagesDirectory, canonicalImagePath) || HasLinkInPath(canonicalImagesDirectory, relativePath))
            throw new InvalidDataException($"Image path for recipe {recipeId} escapes images/.");
        if (!File.Exists(canonicalImagePath))
            throw new InvalidDataException($"Image file for recipe {recipeId} was not found: {file}");
        return canonicalImagePath;
    }

    private static bool IsWithin(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static bool HasLinkInPath(string root, string relativePath)
    {
        var current = root;
        foreach (var part in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if (File.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
            if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
        }
        return false;
    }

    private sealed class RecipeRoot
    {
        [JsonPropertyName("recipes")]
        public List<RecipeEntry?>? Recipes { get; set; }
    }

    private sealed class RecipeEntry
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("ingredients")] public List<string?>? Ingredients { get; set; }
        [JsonPropertyName("steps")] public List<string?>? Steps { get; set; }
        [JsonPropertyName("prep_time_minutes")] public int? PrepTimeMinutes { get; set; }
        [JsonPropertyName("cook_time_minutes")] public int? CookTimeMinutes { get; set; }
        [JsonPropertyName("difficulty")] public string? Difficulty { get; set; }
        [JsonPropertyName("servings")] public int? Servings { get; set; }
    }

    private sealed class ManifestEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("file")] public string? File { get; set; }
        [JsonPropertyName("bytes")] public long? Bytes { get; set; }
    }
}
