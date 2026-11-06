using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Legal;

namespace Jobsy.Core.Rules;

/// <summary>
/// Incl.-btw price for the uitgebreide test, per assessment kind.
/// </summary>
public static class DeepAnalysisPricing
{
    /// <summary>
    /// The waiver sentence's version. Shared with the terms (<see cref="LegalDocumentVersions.Terms"/>),
    /// since the checkbox text (<c>Terms.Waiver.Checkbox</c>) is the gebruiksvoorwaarden's bedenktijd clause.
    /// </summary>
    public static string WaiverTextVersion => LegalDocumentVersions.Terms.Version;

    public static decimal For(FlexCommercialSettingsDto settings, AssessmentKind kind)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return kind switch
        {
            AssessmentKind.Career => settings.DeepTestPriceCareerEuro,
            AssessmentKind.Values => settings.DeepTestPriceValuesEuro,
            AssessmentKind.Culture => settings.DeepTestPriceCultureEuro,
            _ => settings.DeepTestPriceCompetenceEuro
        };
    }

    public static decimal For(FlexCommercialSettings settings, AssessmentKind kind)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return kind switch
        {
            AssessmentKind.Career => settings.DeepTestPriceCareerEuro,
            AssessmentKind.Values => settings.DeepTestPriceValuesEuro,
            AssessmentKind.Culture => settings.DeepTestPriceCultureEuro,
            _ => settings.DeepTestPriceCompetenceEuro
        };
    }

    public static (int ExVatCents, int VatCents, int TotalCents) Split(
        FlexCommercialSettingsDto settings,
        AssessmentKind kind)
        => TokenVatPricing.SplitInclVatEuros(For(settings, kind));

    public static string TestNameNl(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => "Beroepen",
        AssessmentKind.Values => "Waarden",
        AssessmentKind.Culture => "Cultuur",
        _ => "Competenties"
    };

    public static string DescriptionNl(AssessmentKind kind)
        => $"Lobsy Uitgebreide test {TestNameNl(kind)}";
}
