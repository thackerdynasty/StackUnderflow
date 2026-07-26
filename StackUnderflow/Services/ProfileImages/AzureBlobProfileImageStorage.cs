using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Stores profile images as blobs in Azure Storage. Registered only when a
/// connection string is present; otherwise <see cref="NullProfileImageStorage"/>
/// takes its place and the feature stays dormant.
/// </summary>
public sealed class AzureBlobProfileImageStorage : IProfileImageStorage
{
    // Every upload gets a new GUID, so a given URL never changes contents and can
    // be cached hard. Replacing an avatar produces a different URL, which the page
    // picks up immediately.
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<AzureBlobProfileImageStorage> _logger;

    // Guards the one-time CreateIfNotExists so concurrent first uploads don't race.
    private readonly SemaphoreSlim _containerLock = new(1, 1);
    private bool _containerReady;

    public AzureBlobProfileImageStorage(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options,
        ILogger<AzureBlobProfileImageStorage> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => true;

    public async Task<string> UploadAsync(
        string userId,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var relativePath = $"{userId}/{Guid.NewGuid()}{extension}";

        try
        {
            var container = await GetContainerAsync(cancellationToken);

            await container.GetBlobClient(relativePath).UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType,
                        CacheControl = ImmutableCacheControl,
                    },
                },
                cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            // Surfaced as a clear 502 by the endpoint rather than an opaque 500. The
            // usual causes are a container that does not exist and cannot be created,
            // or credentials without permission to write.
            _logger.LogError(
                ex,
                "Failed to upload profile image to container '{Container}'.",
                _options.ResolvedContainerName);

            throw new ProfileImageStorageException(
                $"Azure Storage rejected the upload to container '{_options.ResolvedContainerName}'.",
                ex);
        }

        _logger.LogInformation("Uploaded profile image '{RelativePath}'.", relativePath);
        return relativePath;
    }

    public async Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var container = _blobServiceClient.GetBlobContainerClient(_options.ResolvedContainerName);

        try
        {
            // DeleteIfExists already tolerates a missing blob; the catch also covers the
            // container itself being gone. Cleanup of a replaced image must never fail
            // the request that replaced it, so every storage error here is logged and
            // swallowed — the worst case is one orphaned blob.
            await container.GetBlobClient(relativePath).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Previous profile image '{RelativePath}' was already gone.", relativePath);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(
                ex,
                "Could not delete replaced profile image '{RelativePath}'; it may be left orphaned.",
                relativePath);
        }
    }

    public string? GetUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        // Composed from the account endpoint at read time, so only the relative path
        // is ever persisted and the account can change without a data migration.
        return _blobServiceClient
            .GetBlobContainerClient(_options.ResolvedContainerName)
            .GetBlobClient(relativePath)
            .Uri
            .ToString();
    }

    /// <summary>
    /// Resolves the container, creating it on first use. Deliberately lazy: the app
    /// must start and run normally even when the container does not exist yet.
    /// </summary>
    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_options.ResolvedContainerName);

        if (_containerReady)
        {
            return container;
        }

        await _containerLock.WaitAsync(cancellationToken);
        try
        {
            if (!_containerReady)
            {
                await EnsureContainerAsync(container, cancellationToken);
                _containerReady = true;
            }
        }
        finally
        {
            _containerLock.Release();
        }

        return container;
    }

    private async Task EnsureContainerAsync(BlobContainerClient container, CancellationToken cancellationToken)
    {
        try
        {
            // Blob-level anonymous read lets <img> tags load avatars directly.
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
        {
            // The storage account has anonymous blob access turned off. Fall back to a
            // private container so the upload still succeeds, and say plainly what to
            // change, because avatars will not render until it is enabled.
            _logger.LogWarning(
                "Container '{Container}' was created without anonymous access because the storage account " +
                "disallows it. Profile images will not load in the browser until 'Allow Blob anonymous access' " +
                "is enabled on the storage account. See the README.",
                _options.ResolvedContainerName);

            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        }

        await WarnIfNotPubliclyReadableAsync(container, cancellationToken);
    }

    /// <summary>
    /// CreateIfNotExists never changes the access level of a container that already
    /// exists, and the portal creates containers private by default. Without this
    /// check that combination fails silently: uploads succeed but every image 404s.
    /// Diagnostic only — a failure here must never break an upload.
    /// </summary>
    private async Task WarnIfNotPubliclyReadableAsync(
        BlobContainerClient container,
        CancellationToken cancellationToken)
    {
        try
        {
            var policy = await container.GetAccessPolicyAsync(cancellationToken: cancellationToken);
            if (policy.Value.BlobPublicAccess == PublicAccessType.None)
            {
                _logger.LogWarning(
                    "Container '{Container}' is private, so uploaded profile images will not load in the " +
                    "browser. In the Azure portal set the container's anonymous access level to 'Blob', and " +
                    "make sure 'Allow Blob anonymous access' is enabled on the storage account. See the README.",
                    _options.ResolvedContainerName);
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogDebug(
                ex,
                "Could not read the access level for container '{Container}'.",
                _options.ResolvedContainerName);
        }
    }
}