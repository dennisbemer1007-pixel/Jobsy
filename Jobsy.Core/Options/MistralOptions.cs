using Jobsy.Core.Enums;

namespace Jobsy.Core.Options;

/// <summary>Config keys for <c>Mistral:Models</c>. Values are Mistral model ids.</summary>
public static class MistralFeatureSlots
{
    public const string Story = "Story";
    public const string CareerReport = "CareerReport";
    public const string Compass = "Compass";
    public const string Chat = "Chat";
}

/// <summary>One row on Admin → Integraties.</summary>
public sealed record MistralActiveFeatureModel(string Feature, string Model);

/// <summary>Per-feature Mistral model. Blank means “use <see cref="MistralOptions.Model"/>”.</summary>
public sealed class MistralFeatureModels
{
    public string? Story { get; set; }

    public string? CareerReport { get; set; }

    public string? Compass { get; set; }

    public string? Chat { get; set; }
}

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

    /// <summary>
    /// Optional per-feature overrides. Empty slots use <see cref="Model"/>.
    /// Env: <c>Mistral__Models__Story</c>, <c>__CareerReport</c>, <c>__Compass</c>, <c>__Chat</c>.
    /// </summary>
    public MistralFeatureModels Models { get; set; } = new();

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Model sent for this feature. Other features stay on <see cref="Model"/>.</summary>
    public string ModelFor(OpenAiFeature feature)
    {
        if (feature == OpenAiFeature.CareerCompass)
        {
            return ModelForCompassCall();
        }

        var slot = feature switch
        {
            OpenAiFeature.WhoAmI => Models?.Story,
            OpenAiFeature.AssistantChat => Models?.Chat,
            _ => null
        };
        return Effective(slot, Model);
    }

    /// <summary>
    /// The compass call writes the job list and the career-report sentences.
    /// An explicit compass model wins. Otherwise the career-report model. Otherwise <see cref="Model"/>.
    /// </summary>
    public string ModelForCompassCall()
        => Effective(Models?.Compass, Effective(Models?.CareerReport, Model));

    /// <summary>
    /// What Admin → Integraties shows: the model each surface sends.
    /// The career report and the compass share one call, so both rows show that model.
    /// </summary>
    public IReadOnlyList<MistralActiveFeatureModel> ActiveFeatureModels()
    {
        var compassCall = ModelForCompassCall();
        return
        [
            new(MistralFeatureSlots.Story, ModelFor(OpenAiFeature.WhoAmI)),
            new(MistralFeatureSlots.CareerReport, compassCall),
            new(MistralFeatureSlots.Compass, compassCall),
            new(MistralFeatureSlots.Chat, ModelFor(OpenAiFeature.AssistantChat))
        ];
    }

    public static string Effective(string? slot, string? shared)
    {
        if (!string.IsNullOrWhiteSpace(slot))
        {
            return slot.Trim();
        }

        if (!string.IsNullOrWhiteSpace(shared))
        {
            return shared.Trim();
        }

        return DefaultModel;
    }

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
