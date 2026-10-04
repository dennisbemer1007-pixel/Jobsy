using Jobsy.Core.Options;

namespace Jobsy.Core.Ai;

/// <summary>What the admin integrations page shows for the AI company that actually runs.</summary>
public static class ActiveProviderStatus
{
    public const string RegionEu = "eu";
    public const string RegionGlobal = "global";
    public const string RegionUs = "us";
    public const string RegionOff = "off";

    public static AiRuntimeStatus DescribeAi(
        string? provider,
        string? mistralApiKey,
        string? mistralModel,
        string? mistralBaseUrl,
        string? openAiModel,
        string? openAiBaseUrl)
    {
        var decision = AiProviderChoice.Decide(provider, mistralApiKey);
        if (decision.Kind == AiProviderKind.Mistral)
        {
            var model = string.IsNullOrWhiteSpace(mistralModel)
                ? MistralOptions.DefaultModel
                : mistralModel.Trim();
            var baseUrl = MistralEndpoint.EffectiveBaseUrl(mistralBaseUrl);
            return new AiRuntimeStatus(
                AiProviderNames.Mistral,
                model,
                RegionCode(baseUrl),
                HostOf(baseUrl),
                Available: true);
        }

        if (decision.Kind == AiProviderKind.OpenAI)
        {
            var model = string.IsNullOrWhiteSpace(openAiModel) ? "gpt-4o-mini" : openAiModel.Trim();
            var baseUrl = string.IsNullOrWhiteSpace(openAiBaseUrl)
                ? "https://api.openai.com/v1/"
                : openAiBaseUrl.Trim();
            return new AiRuntimeStatus(
                AiProviderNames.OpenAI,
                model,
                RegionUs,
                HostOf(baseUrl),
                Available: true);
        }

        return new AiRuntimeStatus("Unavailable", null, RegionOff, null, Available: false);
    }

    public static string RegionCode(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return RegionOff;
        }

        if (baseUrl.Contains("api.eu.mistral.ai", StringComparison.OrdinalIgnoreCase))
        {
            return RegionEu;
        }

        if (baseUrl.Contains("mistral.ai", StringComparison.OrdinalIgnoreCase))
        {
            return RegionGlobal;
        }

        if (baseUrl.Contains("openai.com", StringComparison.OrdinalIgnoreCase))
        {
            return RegionUs;
        }

        return RegionGlobal;
    }

    public static string? HostOf(string? baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Host;
    }
}

public sealed record AiRuntimeStatus(
    string Provider,
    string? Model,
    string RegionCode,
    string? EndpointHost,
    bool Available);
