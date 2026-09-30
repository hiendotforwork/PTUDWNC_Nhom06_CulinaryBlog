namespace CulinaryBlog.Infrastructure.Storage;

public sealed class MinioStorageOptions
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool UseSsl { get; set; }
    public string BucketName { get; set; } = "culinary-blog";
    // Externally reachable origin, without bucket; may differ from the internal Endpoint.
    public string PublicBaseUrl { get; set; } = "http://localhost:9000";
}
