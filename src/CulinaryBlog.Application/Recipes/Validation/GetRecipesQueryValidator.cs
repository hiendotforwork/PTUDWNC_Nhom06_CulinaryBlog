using CulinaryBlog.Application.Recipes.Queries;

namespace CulinaryBlog.Application.Recipes.Validation;

public static class GetRecipesQueryValidator
{
    public static readonly string[] AllowedSorts =
    [
        "createdAt",
        "-createdAt",
        "title",
        "-title",
        "cookTime",
        "-cookTime"
    ];

    public static Dictionary<string, string[]> Validate(GetRecipesQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
        {
            errors["page"] = ["page must be at least 1."];
        }

        if (query.PageSize is < 1 or > 50)
        {
            errors["pageSize"] = ["pageSize must be between 1 and 50."];
        }

        if (!AllowedSorts.Contains(query.Sort, StringComparer.OrdinalIgnoreCase))
        {
            errors["sort"] = ["sort must be createdAt, -createdAt, title, -title, cookTime, or -cookTime."];
        }

        if (query.MinPrepTime is < 0)
        {
            errors["minPrepTime"] = ["minPrepTime must be non-negative."];
        }

        if (query.MaxCookTime is < 0)
        {
            errors["maxCookTime"] = ["maxCookTime must be non-negative."];
        }

        if (query.MinServings is < 1)
        {
            errors["minServings"] = ["minServings must be at least 1."];
        }

        return errors;
    }
}