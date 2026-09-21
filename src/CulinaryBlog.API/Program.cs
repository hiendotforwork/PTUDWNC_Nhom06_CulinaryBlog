using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
var labCommand = args.FirstOrDefault(x => x.StartsWith("--lab2-"));
if (labCommand is not null)
{
    if (labCommand is not ("--lab2-migrate" or "--lab2-seed" or "--lab2-verify"))
        throw new ArgumentException("Unknown Lab 2 command.");
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection locally; do not commit credentials.");
    var options = new DbContextOptionsBuilder<CulinaryBlogDbContext>()
        .UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "culinary")).Options;
    await using var db = new CulinaryBlogDbContext(options);
    if (labCommand == "--lab2-migrate") await db.Database.MigrateAsync();
    if (labCommand == "--lab2-seed") await Lab2Seeder.SeedAsync(db);
    if (labCommand != "--lab2-migrate")
        Console.WriteLine(JsonSerializer.Serialize(await Lab2Seeder.VerifyAsync(db), new JsonSerializerOptions { WriteIndented = true }));
    return;
}
var databaseConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(databaseConnection))
    builder.Services.AddDbContext<CulinaryBlogDbContext>(o => o.UseNpgsql(databaseConnection,
        n => n.MigrationsHistoryTable("__EFMigrationsHistory", "culinary")));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
