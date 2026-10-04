namespace CulinaryBlog.Integration.Tests;

using CulinaryBlog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "IntegrationTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TestFileStorage>();
            services.AddSingleton<CulinaryBlog.Application.Interfaces.IFileStorageService>(sp => sp.GetRequiredService<TestFileStorage>());
            services.AddSingleton<CulinaryBlog.Application.Interfaces.IFileDeletionQueue>(sp => sp.GetRequiredService<TestFileStorage>());
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // InMemory cannot validate relational transactions; PostgreSQL checks cover those separately.
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
