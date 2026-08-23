using Azure.Identity;
using Azure.Storage.Blobs;

namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Startup wiring for profile image storage. Chooses the real Azure implementation
/// or the null object based purely on whether the account is reachable at all, so an
/// app with no "AzureStorage" section starts and runs exactly as before.
/// </summary>
public static class ProfileImageStorageExtensions
{
    public static IServiceCollection AddProfileImageStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(AzureStorageOptions.SectionName);
        services.Configure<AzureStorageOptions>(section);

        var options = section.Get<AzureStorageOptions>() ?? new AzureStorageOptions();

        if (!options.IsConfigured)
        {
            // Neither route configured: nothing Azure-related is constructed at all, so
            // a missing or empty section can never throw at startup.
            services.AddSingleton<IProfileImageStorage, NullProfileImageStorage>();
            return services;
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            // Portable route, deliberately first so a self-hoster can override the
            // endpoint shipped in appsettings.json. Works where there is no Azure
            // identity to borrow: another cloud, plain Docker, CI, or Azurite.
            services.AddSingleton(_ => new BlobServiceClient(options.ConnectionString));
        }
        else
        {
            // Preferred wherever an Azure identity exists — managed identity when
            // deployed, 'az login' locally — because no secret is stored anywhere.
            // Needs the "Storage Blob Data Contributor" role on the account.
            services.AddSingleton(_ => new BlobServiceClient(
                new Uri(options.ServiceUri),
                new DefaultAzureCredential()));
        }

        services.AddSingleton<IProfileImageStorage, AzureBlobProfileImageStorage>();
        return services;
    }

    /// <summary>
    /// Writes a single line at startup saying whether uploads are available, so the
    /// dormant state is obvious in the logs rather than only at the failing endpoint.
    /// </summary>
    public static void LogProfileImageStorageStatus(this IHost app)
    {
        var storage = app.Services.GetRequiredService<IProfileImageStorage>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StackUnderflow.ProfileImages");

        if (storage.IsConfigured)
        {
            logger.LogInformation("Profile image storage is configured; upload endpoint enabled.");
        }
        else
        {
            logger.LogInformation("Profile image storage is not configured; upload endpoint disabled.");
        }
    }
}