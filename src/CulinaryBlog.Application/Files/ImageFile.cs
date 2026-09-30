using System.Buffers.Binary;
using CulinaryBlog.Application.Exceptions;

namespace CulinaryBlog.Application.Files;

// Bound the actual stream, not a caller-supplied size or extension.
public static class ImageFile
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public static async Task<MemoryStream> ReadAsync(Stream source, string contentType, string extension, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        try
        {
            var chunk = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + count > MaxBytes) throw new InvalidImageException("Ảnh không được vượt quá 5 MB.");
                await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
            }
            var detected = Detect(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
            if (detected is null || !string.Equals(contentType, detected.Value.Mime, StringComparison.OrdinalIgnoreCase)
                || !Extensions(detected.Value.Mime).Contains(extension.ToLowerInvariant()))
                throw new InvalidImageException("MIME, phần mở rộng và chữ ký ảnh phải khớp JPEG, PNG, WebP hoặc AVIF.");
            buffer.Position = 0;
            return buffer;
        }
        catch { buffer.Dispose(); throw; }
    }

    public static (string Mime, string Extension)? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(new byte[] { 255, 216, 255 })) return ("image/jpeg", ".jpg");
        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", ".png");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)) return ("image/webp", ".webp");
        if (bytes.Length >= 16 && bytes.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(bytes[..4]);
            if (size >= 16 && size <= bytes.Length && size % 4 == 0)
            {
                if (bytes.Slice(8, 4).SequenceEqual("avif"u8)) return ("image/avif", ".avif");
                for (var i = 16; i < size; i += 4)
                    if (bytes.Slice(i, 4).SequenceEqual("avif"u8)) return ("image/avif", ".avif");
            }
        }
        return null;
    }

    private static string[] Extensions(string mime) => mime switch
    {
        "image/jpeg" => [".jpg", ".jpeg"],
        "image/png" => [".png"],
        "image/webp" => [".webp"],
        "image/avif" => [".avif"],
        _ => []
    };
}
