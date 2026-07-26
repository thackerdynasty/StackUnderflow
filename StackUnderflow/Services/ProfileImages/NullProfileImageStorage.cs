using Microsoft.Extensions.Logging;

namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Null-object <see cref="IProfileImageStorage"/> registered when no Azure Storage
/// connection string is present. Deletes and URL lookups are silently inert, so
/// reading users, rendering profiles, and the seeder behave exactly as they do
/// without the feature. Only the upload endpoint asks <see cref="IsConfigured"/>,
/// which keeps null checks out of every other caller.
/// </summary>
public sealed class NullProfileImageStorage(ILogger<NullProfileImageStorage> logger) : IProfileImageStorage
{
    private readonly ILogger<NullProfileImageStorage> _logger = logger;

    public bool IsConfigured => false;

    /// <summary>
    /// Unreachable in normal operation: the upload endpoint checks
    /// <see cref="IsConfigured"/> and returns 503 before getting here. Throwing
    /// guards against a future caller skipping that check and silently persisting
    /// a blob path that points at nothing.
    /// </summary>
    public Task<string> UploadAsync(
        string userId,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "Profile image storage is not configured. Check IProfileImageStorage.IsConfigured before uploading.");

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            _logger.LogDebug(
                "Skipping delete of profile image '{RelativePath}' because storage is not configured.",
                relativePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Nothing is stored, so there is never a URL to compose.</summary>
    public string? GetUrl(string? relativePath) => null;
}