namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Outcome of validating an uploaded profile image. On success it carries the
/// content type and extension derived from the file's own bytes, so nothing the
/// client supplied is ever echoed back into storage.
/// </summary>
public sealed record ProfileImageValidationResult(
    bool IsValid,
    string? Error,
    string? ContentType,
    string? Extension)
{
    public static ProfileImageValidationResult Invalid(string error) => new(false, error, null, null);

    public static ProfileImageValidationResult Valid(string contentType, string extension) =>
        new(true, null, contentType, extension);
}

/// <summary>
/// Server-side checks for profile image uploads. Deliberately has no dependency on
/// storage or on ASP.NET types, so it runs before any storage call and is fully
/// testable without Azure credentials.
/// </summary>
public static class ProfileImageValidator
{
    public const int MaxSizeBytes = 2 * 1024 * 1024;

    // Signatures are matched against the file's actual leading bytes. The client's
    // file name and Content-Type header are never trusted, since either can be set
    // to anything by the caller.
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature = [0x52, 0x49, 0x46, 0x46]; // "RIFF"
    private static readonly byte[] WebpSignature = [0x57, 0x45, 0x42, 0x50]; // "WEBP"

    // Enough for the longest signature: WebP needs "RIFF" plus a 4-byte length plus "WEBP".
    private const int HeaderLength = 12;

    /// <summary>
    /// Validates size and file signature. Rewinds <paramref name="content"/> when it
    /// is seekable, so the caller can hand the same stream straight to storage.
    /// </summary>
    public static async Task<ProfileImageValidationResult> ValidateAsync(
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        if (length <= 0)
        {
            return ProfileImageValidationResult.Invalid("The uploaded file is empty.");
        }

        if (length > MaxSizeBytes)
        {
            return ProfileImageValidationResult.Invalid(
                $"The image is too large. The maximum size is {MaxSizeBytes / (1024 * 1024)} MB.");
        }

        var header = new byte[HeaderLength];
        var read = await content.ReadAtLeastAsync(
            header,
            HeaderLength,
            throwOnEndOfStream: false,
            cancellationToken);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return Detect(header.AsSpan(0, read))
               ?? ProfileImageValidationResult.Invalid("Only JPEG, PNG, and WebP images are allowed.");
    }

    private static ProfileImageValidationResult? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(JpegSignature))
        {
            return ProfileImageValidationResult.Valid("image/jpeg", ".jpg");
        }

        if (header.StartsWith(PngSignature))
        {
            return ProfileImageValidationResult.Valid("image/png", ".png");
        }

        // WebP is a RIFF container: "RIFF", a 4-byte chunk size, then "WEBP".
        if (header.Length >= HeaderLength
            && header.StartsWith(RiffSignature)
            && header[8..HeaderLength].SequenceEqual(WebpSignature))
        {
            return ProfileImageValidationResult.Valid("image/webp", ".webp");
        }

        return null;
    }
}