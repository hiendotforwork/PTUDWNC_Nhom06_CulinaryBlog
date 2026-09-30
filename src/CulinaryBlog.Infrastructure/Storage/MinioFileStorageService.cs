using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Files;
using CulinaryBlog.Application.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CulinaryBlog.Infrastructure.Storage;

public sealed class MinioFileStorageService(IMinioClient client, IOptions<MinioStorageOptions> options) : IFileStorageService
{
    private readonly MinioStorageOptions settings = options.Value;
    private string UrlPrefix => settings.PublicBaseUrl.TrimEnd('/') + "/" + settings.BucketName + "/";

    public async Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken)
    {
        ValidateFolder(folder);
        await using var image = await ImageFile.ReadAsync(stream, contentType, extension, cancellationToken);
        var detected = ImageFile.Detect(image.GetBuffer().AsSpan(0, (int)image.Length))!.Value;
        var key = folder + "/" + Guid.NewGuid().ToString("N") + detected.Extension;
        try
        {
            await client.PutObjectAsync(new PutObjectArgs().WithBucket(settings.BucketName).WithObject(key)
                .WithStreamData(image).WithObjectSize(image.Length).WithContentType(detected.Mime), cancellationToken);
            return new StoredFile(UrlPrefix + key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { throw new FileStorageException("Không thể tải ảnh lên kho tệp.", ex); }
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var key = GetObjectName(url);
        try
        {
            // S3 DELETE is idempotent; no separate existence check/race.
            await client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(settings.BucketName).WithObject(key), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { throw new FileStorageException("Không thể xóa ảnh khỏi kho tệp.", ex); }
    }

    public string GetObjectName(string url)
    {
        if (!url.StartsWith(UrlPrefix, StringComparison.Ordinal)) throw new ArgumentException("URL is not owned by this storage.", nameof(url));
        var key = url[UrlPrefix.Length..];
        var parts = key.Split('/');
        if (parts.Length != 3 || parts[0] is not ("recipes" or "avatars")
            || !SafeSegment(parts[1]) || !SafeSegment(Path.GetFileNameWithoutExtension(parts[2]))
            || Path.GetExtension(parts[2]) is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".avif"))
            throw new ArgumentException("Invalid object name.", nameof(url));
        return key;
    }

    public static void ValidateFolder(string folder)
    {
        var parts = folder.Split('/');
        if (parts.Length != 2 || parts[0] is not ("recipes" or "avatars") || !SafeSegment(parts[1]))
            throw new ArgumentException("Invalid storage folder.", nameof(folder));
    }

    private static bool SafeSegment(string value) => value.Length > 0 &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
