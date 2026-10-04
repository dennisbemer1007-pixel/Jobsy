namespace Jobsy.Core.Options;

/// <summary>
/// Mistral AI (Paris). Used only when <c>Ai:Provider</c> is <c>Mistral</c>.
/// The key is <c>Mistral__ApiKey</c> (or <c>MISTRAL_API_KEY</c>). It is not stored in the admin screen.
/// EU inference is the default base URL (<c>api.eu.mistral.ai</c>). The global host does not promise a place.
/// </summary>
public sealed class MistralOptions
{
    public const string SectionName = "Mistral";

    /// <summary>
    /// Current small alias on docs.mistral.ai (chat and JSON mode). Checked 2026-10-04.
    /// Regional endpoints use the same model ids as the global API. The EU chat examples name
    /// <c>mistral-medium-latest</c> and <c>mistral-large-latest</c>. The docs do not exclude this
    /// small alias, so it stays the default. Confirm it with GET /v1/models on the EU host.
    /// </summary>
    public const string DefaultModel = "mistral-small-latest";

    /// <summary>EU regional endpoint. Inference runs in the EU/EFTA. About 1.1× list price.</summary>
    public const string DefaultBaseUrl = "https://api.eu.mistral.ai/v1/";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = DefaultModel;

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public static bool HasApiKey(string? apiKey) => !string.IsNullOrWhiteSpace(apiKey);

    /// <summary>Accept the common env name when <c>Mistral__ApiKey</c> is empty.</summary>
    public static void ApplyKeyAlias(MistralOptions options, Func<string, string?> read)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return;
        }

        var alt = read("MISTRAL_API_KEY");
        if (!string.IsNullOrWhiteSpace(alt))
        {
            options.ApiKey = alt.Trim();
        }
    }
}
