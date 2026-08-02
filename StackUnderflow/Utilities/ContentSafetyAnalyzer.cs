using Azure;
using Azure.AI.ContentSafety;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using StackUnderflow.Models;

namespace StackUnderflow.Utilities;

public sealed class ContentSafetyAnalyzer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentSafetyAnalyzer> _logger;
    private readonly Lazy<ContentSafetyClient> _client;

    public ContentSafetyAnalyzer(
        IConfiguration configuration,
        ILogger<ContentSafetyAnalyzer> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _client = new Lazy<ContentSafetyClient>(
            CreateClient,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public (bool, ContentSafety) CheckText(string text)
    {
        var contentSafety = AnalyzeText(text);
        var isSafe = TestForBlock(contentSafety);
        return (isSafe, contentSafety);
    }

    private ContentSafety AnalyzeText(string text)
    {
        var request = new AnalyzeTextOptions(text);

        Response<AnalyzeTextResult> response;
        try
        {
            response = _client.Value.AnalyzeText(request);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Content Safety analysis failed with status {StatusCode} and error code {ErrorCode}.",
                ex.Status,
                ex.ErrorCode);
            throw;
        }

        return new ContentSafety(
            violenceSeverity: response.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Violence)?.Severity ?? 0,
            hateSeverity: response.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Hate)?.Severity ?? 0,
            selfHarmSeverity: response.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.SelfHarm)?.Severity ?? 0,
            sexualContentSeverity: response.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Sexual)?.Severity ?? 0
        );
    }

    private ContentSafetyClient CreateClient()
    {
        var keyVaultUri = GetRequiredSetting("KeyVault:VaultUri");
        var contentSafetyEndpoint = GetRequiredSetting("ContentSafety:Endpoint");
        var keySecretName = GetRequiredSetting("ContentSafety:KeySecretName");

        var credential = new DefaultAzureCredential();
        var secretClient = new SecretClient(new Uri(keyVaultUri), credential);
        var key = secretClient.GetSecret(keySecretName).Value.Value;

        return new ContentSafetyClient(
            new Uri(contentSafetyEndpoint),
            new AzureKeyCredential(key));
    }

    private string GetRequiredSetting(string key)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be set before content moderation can run.");
        }

        return value;
    }

    private static bool TestForBlock(ContentSafety contentSafety)
    {
        if (contentSafety.HateSeverity > 1) return false;
        if (contentSafety.SelfHarmSeverity > 1) return false;
        if (contentSafety.ViolenceSeverity > 1) return false;
        if (contentSafety.SexualContentSeverity > 1) return false;
        return true;
    }
}
