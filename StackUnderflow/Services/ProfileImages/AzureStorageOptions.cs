namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Binds the "AzureStorage" configuration section. Two ways to reach the account are
/// supported so the app is deployable by anyone: <see cref="ServiceUri"/> uses the
/// caller's own Azure identity and stores no secret, while
/// <see cref="ConnectionString"/> carries an account key and works on hosts with no
/// Azure identity at all — another cloud, plain Docker, CI, or the Azurite emulator.
/// The URI ships in appsettings.json because it is not a secret; the connection
/// string ships empty so its shape is discoverable, and is never committed.
/// </summary>
public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>Container used when the section leaves <see cref="ContainerName"/> blank.</summary>
    public const string DefaultContainerName = "avatars";

    /// <summary>
    /// Blob service endpoint, for example "https://myaccount.blob.core.windows.net/".
    /// Not a secret: access comes from a role assignment on the account rather than
    /// from anything in configuration.
    /// </summary>
    public string ServiceUri { get; set; } = string.Empty;

    /// <summary>
    /// Account-key connection string. A full-account credential — strictly more
    /// powerful than a scoped API key, so it must never be committed. Set it only
    /// where no Azure identity is available; it takes precedence over
    /// <see cref="ServiceUri"/> so a self-hoster can point the app at their own account.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// True when either way of reaching the account is present. Everything that
    /// touches Azure hangs off this, so an app with both left blank stays fully
    /// dormant instead of failing at startup.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) || !string.IsNullOrWhiteSpace(ServiceUri);

    /// <summary>The container to use, falling back to <see cref="DefaultContainerName"/>.</summary>
    public string ResolvedContainerName =>
        string.IsNullOrWhiteSpace(ContainerName) ? DefaultContainerName : ContainerName.Trim();
}