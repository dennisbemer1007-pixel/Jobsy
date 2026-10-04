namespace Jobsy.Core.Rules;

/// <summary>
/// Employer talent-pool RIASEC labels and the tag filter share one switch:
/// <c>TalentPool:ShowRiasecTagFilter</c> (default off). Competency match tags stay visible.
/// </summary>
public static class TalentPoolRiasecVisibility
{
    public const string ConfigKey = "TalentPool:ShowRiasecTagFilter";

    public static bool IsRiasecCode(string? tag)
        => !string.IsNullOrWhiteSpace(tag)
           && CareerTestCatalog.HollandLetter.ContainsKey(tag.Trim());
}
