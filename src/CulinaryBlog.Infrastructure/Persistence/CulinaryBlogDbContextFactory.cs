using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CulinaryBlogDbContextFactory : IDesignTimeDbContextFactory<CulinaryBlogDbContext>
{
    public CulinaryBlogDbContext CreateDbContext(string[] args)
    {
        // The fallback supports offline migration generation, not a working login.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=culinary_lab2;Username=postgres";
        var options = new DbContextOptionsBuilder<CulinaryBlogDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "culinary"));
        return new CulinaryBlogDbContext(options.Options);
    }
}
