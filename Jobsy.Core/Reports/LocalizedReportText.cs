namespace Jobsy.Core.Reports;

/// <summary>Bilingual AI or template prose stored on the report; resolved at render time.</summary>
public sealed class LocalizedReportText
{
    public string? Nl { get; set; }
    public string? En { get; set; }

    public string Resolve(string? lang)
        => ReportLanguage.IsEnglish(lang)
            ? (string.IsNullOrWhiteSpace(En) ? Nl ?? "" : En)
            : (string.IsNullOrWhiteSpace(Nl) ? En ?? "" : Nl);

    public static LocalizedReportText FromPair(string nl, string en) => new() { Nl = nl, En = en };

    public static LocalizedReportText FromNl(string nl) => new() { Nl = nl, En = nl };
}
