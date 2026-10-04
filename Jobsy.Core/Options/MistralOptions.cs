namespace Jobsy.Core.Options;

/// <summary>
/// Mistral AI (Paris). Used only when <c>Ai:Provider</c> is <c>Mistral</c>.
/// The key is <c>Mistral__ApiKey</c> (or <c>MISTRAL_API_KEY</c>). It is not stored in the admin screen.
/// Data stays in the EU when the Mistral workspace is created with the EU option.
/// </summary>
public sealed class MistralOptions
{
    public const string SectionName = "Mistral";

    /// <summary>
    /// Current small alias on docs.mistral.ai (chat, JSON mode, and vision).
    /// The analogue of gpt-4o-mini for cost. Checked 2026-10-04.
    /// </summary>
    public const string DefaultModel = "mistral-small-latest";

    public const string DefaultBaseUrl = "https://api.mistral.ai/v1/";

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
