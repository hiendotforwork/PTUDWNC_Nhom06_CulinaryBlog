using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Data.Seed;
using CulinaryBlog.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CulinaryBlogDbContext>();
    await DbSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/categories", async (CulinaryBlogDbContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .Where(c => !c.IsDeleted)
        .OrderBy(c => c.Name)
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.ParentCategoryId,
            c.CreatedAt,
            c.UpdatedAt
        })
        .ToListAsync();

    return Results.Ok(categories);
});

app.Run();
