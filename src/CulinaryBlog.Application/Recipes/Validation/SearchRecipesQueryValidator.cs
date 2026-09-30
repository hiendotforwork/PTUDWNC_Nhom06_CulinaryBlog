namespace CulinaryBlog.Application.Recipes.Validation;

public static class SearchRecipesQueryValidator
{
    public static Dictionary<string, string[]> Validate(string? query, int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2 || query.Length > 256 || query.Count(char.IsLetterOrDigit) < 2)
        {
            errors["q"] = ["q must contain at least 2 letters/digits and at most 256 characters."];
        }

        if (page < 1)
        {
            errors["page"] = ["page must be at least 1."];
        }

        if (pageSize is < 1 or > 50)
        {
            errors["pageSize"] = ["pageSize must be between 1 and 50."];
        }

        if (pageSize > 0 && ((long)page - 1) * pageSize > int.MaxValue)
            errors["page"] = ["page exceeds the supported pagination offset."];
        return errors;
    }
}