using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Tests;

/// <summary>
/// Guards the dormant behaviour: with no AzureStorage configuration the app must
/// resolve a working, inert storage service and never construct an Azure client.
/// </summary>
public class ProfileImageStorageRegistrationTests
{
    [Fact]
    public void AddProfileImageStorage_WithNoSectionAtAll_RegistersTheNullObject()
    {
        var provider = Build([]);

        var storage = provider.GetRequiredService<IProfileImageStorage>();

        Assert.IsType<NullProfileImageStorage>(storage);
        Assert.False(storage.IsConfigured);
    }

    [Fact]
    public void AddProfileImageStorage_WithNeitherRouteConfigured_RegistersTheNullObject()
    {
        // Section present but both routes blank, as when a deployment opts the feature off.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ConnectionString"] = "",
            ["AzureStorage:ContainerName"] = "avatars",
        });

        Assert.IsType<NullProfileImageStorage>(provider.GetRequiredService<IProfileImageStorage>());
    }

    [Fact]
    public void AddProfileImageStorage_WithWhitespaceConnectionString_RegistersTheNullObject()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ConnectionString"] = "   ",
        });

        Assert.IsType<NullProfileImageStorage>(provider.GetRequiredService<IProfileImageStorage>());
    }

    [Fact]
    public void AddProfileImageStorage_WhenNotConfigured_DoesNotRegisterABlobServiceClient()
    {
        var provider = Build([]);

        // Nothing Azure-related may be constructed, so a missing connection string
        // can never throw at startup.
        Assert.Null(provider.GetService<BlobServiceClient>());
    }

    [Fact]
    public void AddProfileImageStorage_WithConnectionString_RegistersAzureStorage()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ConnectionString"] = FakeConnectionString,
            ["AzureStorage:ContainerName"] = "avatars",
        });

        var storage = provider.GetRequiredService<IProfileImageStorage>();

        Assert.IsType<AzureBlobProfileImageStorage>(storage);
        Assert.True(storage.IsConfigured);
        Assert.NotNull(provider.GetService<BlobServiceClient>());
    }

    [Fact]
    public void AddProfileImageStorage_WithConnectionString_ComposesUrlsFromTheRelativePath()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ConnectionString"] = FakeConnectionString,
            ["AzureStorage:ContainerName"] = "avatars",
        });

        var storage = provider.GetRequiredService<IProfileImageStorage>();

        // Composing a URL must not require the container to exist or any network call.
        var url = storage.GetUrl("user-1/abc.jpg");

        Assert.Equal("https://devstore.blob.core.windows.net/avatars/user-1/abc.jpg", url);
    }

    [Fact]
    public void AddProfileImageStorage_WithServiceUriOnly_RegistersAzureStorage()
    {
        // The identity-based route: nothing secret in configuration at all.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ServiceUri"] = FakeServiceUri,
            ["AzureStorage:ContainerName"] = "avatars",
        });

        var storage = provider.GetRequiredService<IProfileImageStorage>();

        Assert.IsType<AzureBlobProfileImageStorage>(storage);
        Assert.True(storage.IsConfigured);
        Assert.Equal(
            "https://devstore.blob.core.windows.net/avatars/user-1/abc.jpg",
            storage.GetUrl("user-1/abc.jpg"));
    }

    [Fact]
    public void AddProfileImageStorage_WithBothRoutes_PrefersTheConnectionString()
    {
        // A self-hoster supplying their own key must win over the endpoint shipped in
        // appsettings.json, or they cannot point the app at their own account.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureStorage:ServiceUri"] = "https://shipped-default.blob.core.windows.net/",
            ["AzureStorage:ConnectionString"] = FakeConnectionString,
            ["AzureStorage:ContainerName"] = "avatars",
        });

        var storage = provider.GetRequiredService<IProfileImageStorage>();

        // devstore comes from the connection string; shipped-default would mean the URI won.
        Assert.Equal(
            "https://devstore.blob.core.windows.net/avatars/user-1/abc.jpg",
            storage.GetUrl("user-1/abc.jpg"));
    }

    [Fact]
    public void AzureStorageOptions_FallsBackToTheDefaultContainerName()
    {
        var options = new AzureStorageOptions { ConnectionString = FakeConnectionString };

        Assert.Equal("avatars", options.ResolvedContainerName);
    }

    [Fact]
    public async Task NullProfileImageStorage_IsInertRatherThanFailing()
    {
        var storage = new NullProfileImageStorage(
            LoggerFactory.Create(_ => { }).CreateLogger<NullProfileImageStorage>());

        Assert.Null(storage.GetUrl("user-1/abc.jpg"));
        Assert.Null(storage.GetUrl(null));

        // Deleting is a silent no-op, so callers need no null checks.
        await storage.DeleteAsync("user-1/abc.jpg");
        await storage.DeleteAsync(null);
    }

    // Well-formed but entirely fake: no network call is made when resolving the
    // service or composing a URL, so no real account or credentials are needed.
    private const string FakeConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=devstore;" +
        "AccountKey=bm90LWEtcmVhbC1rZXktZm9yLXRlc3Rpbmctb25seS1wYWRkaW5nMDA=;" +
        "EndpointSuffix=core.windows.net";

    // Same fictional account as above, reached the identity-based way. Resolving the
    // service and composing a URL make no network call, so no credentials are needed.
    private const string FakeServiceUri = "https://devstore.blob.core.windows.net/";

    private static ServiceProvider Build(IEnumerable<KeyValuePair<string, string?>> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProfileImageStorage(configuration);
        return services.BuildServiceProvider();
    }
}