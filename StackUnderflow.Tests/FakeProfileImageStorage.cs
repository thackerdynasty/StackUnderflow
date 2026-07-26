using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Tests;

/// <summary>
/// In-memory <see cref="IProfileImageStorage"/> for tests. Records what was uploaded
/// and deleted so behaviour can be asserted without Azure credentials, and produces
/// deterministic paths instead of GUIDs so assertions stay readable.
/// </summary>
public sealed class FakeProfileImageStorage : IProfileImageStorage
{
    private const string BaseUrl = "https://example.blob.core.windows.net/avatars/";

    public bool IsConfigured { get; init; } = true;

    /// <summary>Relative paths handed back from <see cref="UploadAsync"/>, in order.</summary>
    public List<string> Uploaded { get; } = [];

    /// <summary>Relative paths passed to <see cref="DeleteAsync"/>, in order.</summary>
    public List<string> Deleted { get; } = [];

    /// <summary>Bytes received by the most recent upload, to prove the stream was readable.</summary>
    public byte[]? LastUploadedBytes { get; private set; }

    public string? LastContentType { get; private set; }

    /// <summary>When set, <see cref="UploadAsync"/> throws it instead of storing.</summary>
    public Exception? UploadException { get; set; }

    public async Task<string> UploadAsync(
        string userId,
        Stream content,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (UploadException is not null)
        {
            throw UploadException;
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        LastUploadedBytes = buffer.ToArray();
        LastContentType = contentType;

        var relativePath = $"{userId}/image-{Uploaded.Count + 1}{extension}";
        Uploaded.Add(relativePath);
        return relativePath;
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            Deleted.Add(relativePath);
        }

        return Task.CompletedTask;
    }

    public string? GetUrl(string? relativePath) =>
        IsConfigured && !string.IsNullOrWhiteSpace(relativePath) ? BaseUrl + relativePath : null;
}