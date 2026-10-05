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

    public const string DefaultModel = "gpt-4o-mini";

    public bool Enabled { get; set; } = true;

    /// <summary>Cheap batch model. A Mistral model name is ignored.</summary>
    public string Model { get; set; } = DefaultModel;

    /// <summary>Pause between OpenAI calls so a full ESCO run can resume without bursting.</summary>
    public int DelayMilliseconds { get; set; } = 250;
}
