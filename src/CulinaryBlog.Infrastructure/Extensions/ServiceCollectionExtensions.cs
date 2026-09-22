using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=CulinaryBlog;Username=postgres;Password=postgres";

        services.AddDbContext<CulinaryBlogDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
