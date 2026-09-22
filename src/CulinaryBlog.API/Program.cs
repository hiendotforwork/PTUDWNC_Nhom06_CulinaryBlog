using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Application.Recipes.Validation;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<CulinaryBlogDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=culinaryblog;Username=postgres;Password=postgres"));
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(GetRecipesQuery).Assembly));
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/v1/recipes", async (
    string? page,
    string? pageSize,
    string? categoryId,
    string? difficulty,
    string? minPrepTime,
    string? maxCookTime,
    string? minServings,
    string? sort,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();
    var parsedPage = ParseInteger(page, 1, "page", errors);
    var parsedPageSize = ParseInteger(pageSize, 12, "pageSize", errors);
    var parsedMinPrepTime = ParseOptionalInteger(minPrepTime, "minPrepTime", errors);
    var parsedMaxCookTime = ParseOptionalInteger(maxCookTime, "maxCookTime", errors);
    var parsedMinServings = ParseOptionalInteger(minServings, "minServings", errors);

    Guid? parsedCategoryId = null;
    if (!string.IsNullOrWhiteSpace(categoryId))
    {
        if (Guid.TryParse(categoryId, out var category))
        {
            parsedCategoryId = category;
        }
        else
        {
            errors["categoryId"] = ["categoryId must be a valid GUID."];
        }
    }

    var parsedDifficulty = string.IsNullOrWhiteSpace(difficulty) ? null : difficulty;
    if (parsedDifficulty is not null
        && !Enum.TryParse<RecipeDifficulty>(parsedDifficulty, true, out _))
    {
        errors["difficulty"] = ["difficulty must be Easy, Medium, or Hard."];
    }

    if (parsedPage is null || parsedPage < 1)
    {
        errors["page"] = ["page must be at least 1."];
    }

    if (parsedPageSize is null || parsedPageSize is < 1 or > 50)
    {
        errors["pageSize"] = ["pageSize must be between 1 and 50."];
    }

    var parsedSort = string.IsNullOrWhiteSpace(sort) ? "-createdAt" : sort;

    var request = new GetRecipesQuery(
        parsedPage!.Value,
        parsedPageSize!.Value,
        parsedCategoryId,
        parsedDifficulty,
        parsedMinPrepTime,
        parsedMaxCookTime,
        parsedMinServings,
        parsedSort);

    foreach (var (field, messages) in GetRecipesQueryValidator.Validate(request))
    {
        errors[field] = messages;
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var result = await sender.Send(request, cancellationToken);

    return Results.Ok(result);
})
.WithName("GetRecipes");

app.MapGet("/api/v1/recipes/search", async (
    string? q,
    string? page,
    string? pageSize,
    ISender sender,
    CancellationToken cancellationToken) =>
{
    var parsedPage = ParseInteger(page, 1, "page", out var pageError);
    var parsedPageSize = ParseInteger(pageSize, 12, "pageSize", out var pageSizeError);
    var errors = SearchRecipesQueryValidator.Validate(q, parsedPage, parsedPageSize);

    if (pageError is not null)
    {
        errors["page"] = [pageError];
    }

    if (pageSizeError is not null)
    {
        errors["pageSize"] = [pageSizeError];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var result = await sender.Send(
        new SearchRecipesQuery(q!.Trim(), parsedPage, parsedPageSize),
        cancellationToken);

    return Results.Ok(result);
})
.WithName("SearchRecipes");

app.Run();

static int? ParseInteger(string? value, int defaultValue, string fieldName, Dictionary<string, string[]> errors)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return defaultValue;
    }

    return int.TryParse(value, out var parsed)
        ? parsed
        : AddParseError(fieldName, errors);
}

static int ParseInteger(string? value, int defaultValue, string fieldName, out string? error)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        error = null;
        return defaultValue;
    }

    if (int.TryParse(value, out var parsed))
    {
        error = null;
        return parsed;
    }

    error = $"{fieldName} must be a valid integer.";
    return defaultValue;
}

static int? ParseOptionalInteger(string? value, string fieldName, Dictionary<string, string[]> errors)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    return int.TryParse(value, out var parsed)
        ? parsed
        : AddParseError(fieldName, errors);
}

static int? AddParseError(string fieldName, Dictionary<string, string[]> errors)
{
    errors[fieldName] = [$"{fieldName} must be a valid integer."];
    return null;
}
