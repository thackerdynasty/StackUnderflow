namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Stores profile images outside the database. The database only ever holds the
/// relative blob path returned by <see cref="UploadAsync"/> (for example
/// "42/9f3c-....jpg"); the absolute URL is composed at read time by
/// <see cref="GetUrl"/> so the storage account can move, or a CDN can be added,
/// without a data migration.
/// </summary>
public interface IProfileImageStorage
{
    /// <summary>
    /// False when no storage is configured. Only the upload endpoint consults this,
    /// so it can answer 503 with a helpful message; every other member is safe to
    /// call in either state and callers never need a null check.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Uploads an already-validated image and returns its relative blob path.
    /// Each upload gets a fresh GUID so the URL changes every time, which lets
    /// avatars be served with long cache headers while a replacement still shows
    /// up immediately.
    /// </summary>
    Task<string> UploadAsync(
        string userId,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously uploaded blob so replaced images do not accumulate.
    /// A path that is null, blank, or already gone is not an error.
    /// </summary>
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Composes the absolute URL for a stored relative path, or null when there is
    /// nothing to show (no path stored, or no storage configured).
    /// </summary>
    string? GetUrl(string? relativePath);
}