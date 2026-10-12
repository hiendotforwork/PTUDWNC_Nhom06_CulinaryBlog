using System.Text.Json;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using CulinaryBlog.Infrastructure.Storage;

namespace CulinaryBlog.API.Services;

public static class MinioBucketInitialization
{
    // Explicit opt-in for a dedicated local development bucket.
    public static async Task InitializeLocalBucketAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue("MinIO:InitializeBucket", false)) return;
        var client = app.Services.GetRequiredService<IMinioClient>();
        var bucket = app.Services.GetRequiredService<IOptions<MinioStorageOptions>>().Value.BucketName;
        if (!await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket)))
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        var policy = JsonSerializer.Serialize(new { Version = "2012-10-17", Statement = new[] {
            new { Effect = "Allow", Principal = new { AWS = new[] { "*" } }, Action = new[] { "s3:GetObject" },
                Resource = new[] { "arn:aws:s3:::" + bucket + "/*" } }
        }});
        await client.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy));
    }
}
