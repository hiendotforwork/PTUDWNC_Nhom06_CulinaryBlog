// Tệp khởi động API với PostgreSQL dùng chung cho Identity và Recipe.
// Khi chạy bằng Docker, ứng dụng tự áp dụng migration, seed Lab 2 và phục vụ ảnh từ Docker Volume.

using MediatR;
using System.Text.Json;
using CulinaryBlog.Application.Recipes.Validation;
using System.Text;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.API.Services;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Recipes;
using CulinaryBlog.Infrastructure.Services;
using CulinaryBlog.Infrastructure.Services.Email;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using AuthApplicationUser = CulinaryBlog.Domain.Entities.ApplicationUser;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=127.0.0.1;Port=55432;Database=culinary_blog;Username=culinary;Password=culinary_dev_password";

var labCommand = args.FirstOrDefault(x => x.StartsWith("--lab2-"));
if (labCommand is not null)
{
    if (labCommand is not ("--lab2-migrate" or "--lab2-seed" or "--lab2-verify"))
        throw new ArgumentException("Unknown Lab 2 command.");
    var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
    await using var db = new ApplicationDbContext(options);
    if (labCommand == "--lab2-migrate")
    {
        await RegisterExistingDockerDatabaseAsync(db);
        await db.Database.MigrateAsync();
    }
    if (labCommand == "--lab2-seed") await Lab2Seeder.SeedAsync(db);
    if (labCommand != "--lab2-migrate")
        Console.WriteLine(JsonSerializer.Serialize(await Lab2Seeder.VerifyAsync(db), new JsonSerializerOptions { WriteIndented = true }));
    return;
}
if (!isTesting)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

}

builder.Services.AddIdentityCore<AuthApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeCommandRepository, RecipeCommandRepository>();
builder.Services.AddScoped<IRecipeUnitOfWork, RecipeUnitOfWork>();
builder.Services.AddFileStorage(builder.Configuration, builder.Environment);
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(GetRecipesQuery).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);

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
var defaultPermitLimit = isTesting ? 1000 : 10;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("RegisterRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = defaultPermitLimit, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("LoginRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = defaultPermitLimit, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new Microsoft.AspNetCore.Mvc.UnprocessableEntityObjectResult(new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState));
});
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!isTesting && builder.Configuration.GetValue("Database:AutoMigrate", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await RegisterExistingDockerDatabaseAsync(database);
    await database.Database.MigrateAsync();
    if (builder.Configuration.GetValue("Database:SeedLab2", false))
        await Lab2Seeder.SeedAsync(database);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
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

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();

// Ghi nhận migration nền mới khi Docker Volume đã có đủ bảng từ migration cũ.
// Input: ApplicationDbContext kết nối database hiện tại. Output: lịch sử migration được bổ sung mà không xóa dữ liệu.
static async Task RegisterExistingDockerDatabaseAsync(ApplicationDbContext database)
{
    await database.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
            "MigrationId" character varying(150) NOT NULL,
            "ProductVersion" character varying(32) NOT NULL,
            CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
        );

        DO $EF$
        DECLARE identity_table text;
        BEGIN
            FOREACH identity_table IN ARRAY ARRAY[
                'AspNetRoles', 'AspNetUsers', 'AspNetRoleClaims', 'AspNetUserClaims',
                'AspNetUserLogins', 'AspNetUserRoles', 'AspNetUserTokens', 'RefreshTokens'
            ] LOOP
                IF to_regclass(format('culinary.%I', identity_table)) IS NOT NULL
                   AND to_regclass(format('public.%I', identity_table)) IS NULL THEN
                    EXECUTE format('ALTER TABLE culinary.%I SET SCHEMA public', identity_table);
                END IF;
            END LOOP;
        END $EF$;

        DO $EF$
        BEGIN
            IF to_regclass('public."AspNetUsers"') IS NOT NULL
               AND to_regclass('public."AspNetRoles"') IS NOT NULL
               AND to_regclass('public."RefreshTokens"') IS NOT NULL
               AND to_regclass('culinary."Categories"') IS NOT NULL
               AND to_regclass('culinary."Recipes"') IS NOT NULL
               AND to_regclass('culinary."RecipeIngredients"') IS NOT NULL
               AND to_regclass('culinary."RecipeSteps"') IS NOT NULL
               AND to_regclass('culinary."RecipeImages"') IS NOT NULL THEN
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('20260930100715_InitialDockerDatabase', '10.0.12')
                ON CONFLICT ("MigrationId") DO NOTHING;
            END IF;
        END $EF$;
        """);
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

public partial class Program { }