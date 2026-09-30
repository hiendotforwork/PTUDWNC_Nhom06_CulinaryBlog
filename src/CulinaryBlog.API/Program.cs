// Tệp khởi động API với PostgreSQL dùng chung cho Identity và Recipe.
// Khi chạy bằng Docker, ứng dụng tự áp dụng migration, seed Lab 2 và phục vụ ảnh từ Docker Volume.

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

if (!isTesting)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddDbContext<CulinaryBlogDbContext>(options => options.UseNpgsql(connectionString));
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
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
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

builder.Services.AddControllers();
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
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();

public partial class Program { }