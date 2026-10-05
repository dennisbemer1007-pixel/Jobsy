using Jobsy.Core.Ai;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Options;

/// <summary>Config keys for <c>Mistral:Models</c>. Values are Mistral model ids.</summary>
public static class MistralFeatureSlots
{
    public const string Story = "Story";
    public const string CareerReport = "CareerReport";
    public const string Compass = "Compass";
    public const string Chat = "Chat";
    public const string CareerPath = "CareerPath";
    public const string CompetenceReport = "CompetenceReport";
    public const string CultureFit = "CultureFit";
    public const string RoleFit = "RoleFit";
    public const string MockInterview = "MockInterview";
    public const string VacancyModeration = "VacancyModeration";
    public const string CvExtraction = "CvExtraction";
    public const string Translation = "Translation";
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

    public string? CareerPath { get; set; }

    public string? CompetenceReport { get; set; }

    public string? CultureFit { get; set; }

    public string? RoleFit { get; set; }
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

    /// <summary>Quality model. Cheap features use <see cref="SmallModel"/> when that is set.</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>
    /// Optional cheaper model for translation, CV extraction and vacancy moderation.
    /// Empty means those features use <see cref="Model"/>.
    /// Env: <c>Mistral__SmallModel</c>. <c>Ai__SmallModel</c> and then <c>OpenAI__SmallModel</c>
    /// are used only when this is empty (see <see cref="ModelFor"/>).
    /// </summary>
    public string? SmallModel { get; set; }

    /// <summary>
    /// Optional per-feature overrides for quality calls. Empty slots use <see cref="Model"/>.
    /// Env: <c>Mistral__Models__Story</c>, <c>__CareerReport</c>, <c>__Compass</c>, <c>__Chat</c>,
    /// <c>__CareerPath</c>, <c>__CompetenceReport</c>, <c>__CultureFit</c>, <c>__RoleFit</c>.
    /// </summary>
    public MistralFeatureModels Models { get; set; } = new();

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// Model sent for this feature.
    /// Cheap features use <see cref="SmallModel"/>, then <paramref name="smallModelFallback"/>, then <see cref="Model"/>.
    /// Quality features use their slot, then <see cref="Model"/>.
    /// </summary>
    public string ModelFor(OpenAiFeature feature, string? smallModelFallback = null)
    {
        if (AiModelRouting.UsesSmallModel(feature))
        {
            return Effective(AiModelRouting.FirstNonEmpty(SmallModel, smallModelFallback), Model);
        }

        if (feature == OpenAiFeature.CareerCompass)
        {
            return ModelForCompassCall();
        }

        var slot = feature switch
        {
            OpenAiFeature.WhoAmI => Models?.Story,
            OpenAiFeature.AssistantChat => Models?.Chat,
            OpenAiFeature.CareerPathPlan => Models?.CareerPath,
            OpenAiFeature.CompetenceDeepReport => Models?.CompetenceReport,
            OpenAiFeature.CultureFit => Models?.CultureFit,
            OpenAiFeature.RoleFitCheck => Models?.RoleFit,
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
    public IReadOnlyList<MistralActiveFeatureModel> ActiveFeatureModels(string? smallModelFallback = null)
    {
        var compassCall = ModelForCompassCall();
        return
        [
            new(MistralFeatureSlots.Story, ModelFor(OpenAiFeature.WhoAmI, smallModelFallback)),
            new(MistralFeatureSlots.CareerReport, compassCall),
            new(MistralFeatureSlots.Compass, compassCall),
            new(MistralFeatureSlots.Chat, ModelFor(OpenAiFeature.AssistantChat, smallModelFallback)),
            new(MistralFeatureSlots.CareerPath, ModelFor(OpenAiFeature.CareerPathPlan, smallModelFallback)),
            new(MistralFeatureSlots.CompetenceReport, ModelFor(OpenAiFeature.CompetenceDeepReport, smallModelFallback)),
            new(MistralFeatureSlots.CultureFit, ModelFor(OpenAiFeature.CultureFit, smallModelFallback)),
            new(MistralFeatureSlots.RoleFit, ModelFor(OpenAiFeature.RoleFitCheck, smallModelFallback)),
            new(MistralFeatureSlots.MockInterview, ModelFor(OpenAiFeature.MockInterview, smallModelFallback)),
            new(MistralFeatureSlots.VacancyModeration, ModelFor(OpenAiFeature.VacancyContentModeration, smallModelFallback)),
            new(MistralFeatureSlots.CvExtraction, ModelFor(OpenAiFeature.CvExtraction, smallModelFallback)),
            new(MistralFeatureSlots.Translation, ModelFor(OpenAiFeature.Translation, smallModelFallback))
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
