using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Minio;

namespace CulinaryBlog.API.Services;

public static class StorageRegistration
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            return services; // Test host supplies its own deletion queue.
        }

        var provider = configuration["FileStorage:Provider"] ?? "Minio";
        if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase) && environment.IsDevelopment())
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        else if (provider.Equals("Minio", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOptions<MinioStorageOptions>().Bind(configuration.GetSection("MinIO"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint) && !o.Endpoint.Contains("://"), "MinIO:Endpoint must be host:port.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.AccessKey) && !string.IsNullOrWhiteSpace(o.SecretKey), "Set MinIO credentials via environment or user secrets.")
                .Validate(o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out var u) && u.Scheme is "http" or "https" && string.IsNullOrEmpty(u.Query) && string.IsNullOrEmpty(u.Fragment) && string.IsNullOrEmpty(u.UserInfo), "MinIO:PublicBaseUrl must be an HTTP(S) URL without credentials, query or fragment.")
                .Validate(o => System.Text.RegularExpressions.Regex.IsMatch(o.BucketName, "^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$"), "Invalid MinIO bucket name.")
                .ValidateOnStart();
            services.AddSingleton<IMinioClient>(sp =>
            {
                var o = sp.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
                return new MinioClient().WithEndpoint(o.Endpoint).WithCredentials(o.AccessKey, o.SecretKey).WithSSL(o.UseSsl).Build();
            });
            services.AddSingleton<IFileStorageService, MinioFileStorageService>();
        }
        else throw new InvalidOperationException("FileStorage:Provider must be Minio (or Local in Development).");

        var connection = configuration.GetConnectionString("HangfireConnection") ?? configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure DefaultConnection or HangfireConnection for durable file deletion jobs.");
        services.AddHangfire(config => config.UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connection)));
        services.AddHangfireServer();
        services.AddScoped<IFileDeletionQueue, HangfireFileDeletionQueue>();
        services.AddScoped<FileDeletionJob>();
        return services;
    }
}
