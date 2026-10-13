namespace Jobsy.Core.Reports;

/// <summary>
/// Maps UI culture to paid-report language. EN → English; everything else → Dutch for now
/// (pl/ro/ar report catalogs are a later follow-up).
/// </summary>
public static class ReportLanguage
{
    public const string Nl = "nl";
    public const string En = "en";

    public static string FromUi(string? uiLanguage)
        => string.Equals(uiLanguage?.Trim(), En, StringComparison.OrdinalIgnoreCase) ? En : Nl;

    public static bool IsEnglish(string? lang)
        => string.Equals(FromUi(lang), En, StringComparison.OrdinalIgnoreCase);
}
