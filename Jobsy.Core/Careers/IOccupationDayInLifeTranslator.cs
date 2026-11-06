namespace Jobsy.Core.Careers;

/// <summary>
/// Translates one stored Dutch workday into another supported language.
/// Called only from the admin one-shot. The candidate read path does not use this.
/// </summary>
public interface IOccupationDayInLifeTranslator
{
    Task<OccupationDayTranslateResult> TranslateAsync(
        OccupationDayDraft source,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}

public sealed record OccupationDayTranslateResult(
    bool Ok,
    OccupationDayDraft? Draft,
    string? Error,
    string Model,
    bool KeyMissing);
