using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Application.Recipes.Validation;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Services;
using CulinaryBlog.Infrastructure.Services.Email;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AuthApplicationUser = CulinaryBlog.Domain.Entities.ApplicationUser;

var builder = WebApplication.CreateBuilder(args);
// Chức năng: nhận lệnh --lab2-migrate, --lab2-seed hoặc --lab2-verify để thao tác database.
// Input: args của tiến trình và DefaultConnection. Output: migration/dữ liệu mẫu/kết quả kiểm tra rồi kết thúc tiến trình.
var labCommand = args.FirstOrDefault(x => x.StartsWith("--lab2-"));
if (labCommand is not null)
{
    if (labCommand is not ("--lab2-migrate" or "--lab2-seed" or "--lab2-verify"))
        throw new ArgumentException("Unknown Lab 2 command.");
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection locally; do not commit credentials.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
    await using var db = new ApplicationDbContext(options);
    if (labCommand == "--lab2-migrate") await db.Database.MigrateAsync();
    if (labCommand == "--lab2-seed") await Lab2Seeder.SeedAsync(db);
    if (labCommand != "--lab2-migrate")
        Console.WriteLine(JsonSerializer.Serialize(await Lab2Seeder.VerifyAsync(db), new JsonSerializerOptions { WriteIndented = true }));
    return;
}
// Add services to the container.
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            options.UseNpgsql(connectionString);
        }
    });
}

// ASP.NET Core Identity Configuration
builder.Services.AddIdentityCore<AuthApplicationUser>(options =>
{
    // Password settings (BR-AUTH-002)
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // Lockout settings (BR-AUTH-007)
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Application Services & Repositories DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// MediatR & FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? builder.Configuration["Jwt:AccessTokenSecret"]
    ?? "CulinaryBlogDefaultSuperSecretKeyForDevelopmentAndTestingPurposesOnly32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CulinaryBlog";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CulinaryBlogApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Rate Limiter Configuration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("RegisterRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<CulinaryBlogDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=culinaryblog;Username=postgres;Password=postgres"));
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(GetRecipesQuery).Assembly));
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

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
app.MapControllers();
app.Run();

// Partial Program class for WebApplicationFactory in integration tests
public partial class Program { }
