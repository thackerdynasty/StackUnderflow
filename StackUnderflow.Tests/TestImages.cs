using Microsoft.AspNetCore.Http;

namespace StackUnderflow.Tests;

/// <summary>
/// Builds <see cref="IFormFile"/> instances with real file signatures, so the
/// validator is exercised against actual magic bytes rather than mocked results.
/// </summary>
public static class TestImages
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] GifHeader = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]; // "GIF89a"

    public static IFormFile Png(int totalBytes = 64, string fileName = "avatar.png", string contentType = "image/png") =>
        Create(PngHeader, totalBytes, fileName, contentType);

    public static IFormFile Jpeg(int totalBytes = 64, string fileName = "avatar.jpg", string contentType = "image/jpeg") =>
        Create(JpegHeader, totalBytes, fileName, contentType);

    public static IFormFile WebP(int totalBytes = 64, string fileName = "avatar.webp", string contentType = "image/webp")
    {
        // RIFF container: "RIFF", a 4-byte size, then "WEBP".
        var header = new byte[12];
        "RIFF"u8.CopyTo(header);
        BitConverter.GetBytes(totalBytes - 8).CopyTo(header, 4);
        "WEBP"u8.CopyTo(header.AsSpan(8));
        return Create(header, totalBytes, fileName, contentType);
    }

    /// <summary>A format the endpoint must reject, regardless of what the client claims it is.</summary>
    public static IFormFile Gif(int totalBytes = 64, string fileName = "avatar.gif", string contentType = "image/gif") =>
        Create(GifHeader, totalBytes, fileName, contentType);

    private static IFormFile Create(byte[] header, int totalBytes, string fileName, string contentType)
    {
        var bytes = new byte[Math.Max(totalBytes, header.Length)];
        header.CopyTo(bytes, 0);

        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}