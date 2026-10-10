namespace Jobsy.Core.Options;

/// <summary>
/// Batch settings for a typical workday per ESCO occupation.
/// The candidate page reads <see cref="EnabledKey"/> (default on). The fill always uses OpenAI, never Mistral.
/// </summary>
public sealed class OccupationDayInLifeOptions
{
    public const string SectionName = "OccupationDayInLife";

    /// <summary>Env: <c>OccupationDayInLifeEnabled</c>. Missing means on.</summary>
    public const string EnabledKey = "OccupationDayInLifeEnabled";

    /// <summary>Default quality model for day-page generation (OpenAI only).</summary>
    public const string DefaultModel = "gpt-4o";

    public bool Enabled { get; set; } = true;

    /// <summary>Batch model. Env: <c>OccupationDayInLife__Model</c>. Mistral names are ignored.</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>Pause between OpenAI calls so a full ESCO run can resume without bursting.</summary>
    public int DelayMilliseconds { get; set; } = 250;

    /// <summary>Generation attempts per occupation when validation or quality checks fail (2–5).</summary>
    public int MaxComposeRetries { get; set; } = 4;

    /// <summary>Optional second OpenAI pass to flag unnatural Dutch before store. Off by default for batch cost.</summary>
    public bool QualityReviewWithAi { get; set; }
}
