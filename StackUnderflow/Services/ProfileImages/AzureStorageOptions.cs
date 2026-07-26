namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Binds the "AzureStorage" configuration section. The section ships in
/// appsettings.json with an empty connection string so the required shape is
/// discoverable in source control; the real value comes from User Secrets locally
/// and from app settings when deployed, and is never committed.
/// </summary>
public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>Container used when the section leaves <see cref="ContainerName"/> blank.</summary>
    public const string DefaultContainerName = "avatars";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// True only when a connection string is actually present. Everything that
    /// touches Azure hangs off this, so an unconfigured app stays fully dormant
    /// instead of failing at startup.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>The container to use, falling back to <see cref="DefaultContainerName"/>.</summary>
    public string ResolvedContainerName =>
        string.IsNullOrWhiteSpace(ContainerName) ? DefaultContainerName : ContainerName.Trim();
}