using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;

namespace CulinaryBlog.API.Services;

public sealed class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return HealthCheckResult.Unhealthy("Database is unavailable."); }
    }
}
public sealed class RedisHealthCheck(IServiceProvider services) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().PingAsync().WaitAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return HealthCheckResult.Unhealthy("Redis is unavailable."); }
    }
}
public sealed class StorageHealthCheck(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            if ((configuration["FileStorage:Provider"] ?? "Minio").Equals("Local", StringComparison.OrdinalIgnoreCase))
                return Directory.Exists(Path.Combine(environment.ContentRootPath, "wwwroot"))
                    ? HealthCheckResult.Healthy("Local development storage.") : HealthCheckResult.Unhealthy("Local storage is unavailable.");
            var client = services.GetRequiredService<IMinioClient>();
            var settings = services.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
            return await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(settings.BucketName), ct)
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Storage bucket is unavailable.");
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return HealthCheckResult.Unhealthy("Object storage is unavailable."); }
    }
}
