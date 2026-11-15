namespace Jobsy.Core.WerkgeverApk;

public enum WerkgeverApkManagerQuestionCategory
{
    Competence,
    Values
}

/// <summary>
/// Ten manager recognition questions (vragen-leidinggevende.md) mapped to employee MBO blocks.
/// </summary>
public sealed record WerkgeverApkManagerQuestion(
    string Id,
    string Label,
    WerkgeverApkManagerQuestionCategory Category,
    string Scenario,
    string? EmployeeCompetenceDimension,
    string? EmployeeValueBarKey);

public static class WerkgeverApkManagerQuestionCatalog
{
    public static IReadOnlyList<WerkgeverApkManagerQuestion> All { get; } =
    [
        new(
            "q1",
            "Samen & aardig",
            WerkgeverApkManagerQuestionCategory.Competence,
            "Je medewerker werkt prettig samen en houdt rekening met collega's.",
            EmployeeCompetenceDimension: "Samenwerken",
            EmployeeValueBarKey: null),
        new(
            "q2",
            "Afmaken & netjes werken",
            WerkgeverApkManagerQuestionCategory.Competence,
            "Je medewerker maakt af wat is afgesproken en levert netjes werk af.",
            EmployeeCompetenceDimension: "Resultaatgerichtheid",
            EmployeeValueBarKey: null),
        new(
            "q3",
            "Kalm blijven",
            WerkgeverApkManagerQuestionCategory.Competence,
            "Je medewerker blijft rustig als het druk of lastig wordt.",
            EmployeeCompetenceDimension: "Stressbestendigheid",
            EmployeeValueBarKey: null),
        new(
            "q4",
            "Nieuwe dingen proberen",
            WerkgeverApkManagerQuestionCategory.Competence,
            "Je medewerker probeert graag nieuwe manieren of taken uit.",
            EmployeeCompetenceDimension: "Creativiteit / vernieuwen (Innovatie)",
            EmployeeValueBarKey: null),
        new(
            "q5",
            "Energie van mensen",
            WerkgeverApkManagerQuestionCategory.Competence,
            "Je medewerker haalt energie uit contact met collega's en klanten.",
            EmployeeCompetenceDimension: "Contact maken (Extraversie)",
            EmployeeValueBarKey: null),
        new(
            "q6",
            "Zelf kiezen en uitdaging",
            WerkgeverApkManagerQuestionCategory.Values,
            "Je medewerker wil zelf keuzes maken en graag iets nieuws aangaan.",
            EmployeeCompetenceDimension: null,
            EmployeeValueBarKey: "Zelfstandigheid (Autonomy)"),
        new(
            "q7",
            "Verbinding & zorg",
            WerkgeverApkManagerQuestionCategory.Values,
            "Een warme band met collega's geeft je medewerker energie.",
            EmployeeCompetenceDimension: null,
            EmployeeValueBarKey: "Verbinding (Connection)"),
        new(
            "q8",
            "Prestatie & groei",
            WerkgeverApkManagerQuestionCategory.Values,
            "Je medewerker wil laten zien wat hij of zij kan en doorgroeien.",
            EmployeeCompetenceDimension: null,
            EmployeeValueBarKey: "Prestatie (Achievement)"),
        new(
            "q9",
            "Zekerheid & duidelijkheid",
            WerkgeverApkManagerQuestionCategory.Values,
            "Duidelijke afspraken en zekerheid helpen je medewerker om goed te werken.",
            EmployeeCompetenceDimension: null,
            EmployeeValueBarKey: "Zekerheid (Stability)"),
        new(
            "q10",
            "Impact & rechtvaardigheid",
            WerkgeverApkManagerQuestionCategory.Values,
            "Je medewerker vindt het belangrijk om bij te dragen aan iets goeds of eerlijks.",
            EmployeeCompetenceDimension: null,
            EmployeeValueBarKey: "Betekenis (Impact)")
    ];

    public static WerkgeverApkManagerQuestion? ById(string questionId)
        => All.FirstOrDefault(q => string.Equals(q.Id, questionId, StringComparison.OrdinalIgnoreCase));
}