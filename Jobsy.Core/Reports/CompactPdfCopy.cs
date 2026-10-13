namespace Jobsy.Core.Reports;

/// <summary>
/// Short chrome labels for the compact deep-test PDF. Report body copy stays in the
/// existing nl/en catalog; these headings are the new strings and exist in all five UI languages.
/// </summary>
public static class CompactPdfCopy
{
    public static string Profile(string? uiLang) => Pick(uiLang,
        "Jouw profiel",
        "Your profile",
        "Twój profil",
        "Profilul tău",
        "ملفك");

    public static string TopThree(string? uiLang) => Pick(uiLang,
        "Sterkste drie",
        "Strongest three",
        "Trzy najsilniejsze",
        "Cele mai puternice trei",
        "أقوى ثلاثة");

    public static string WhatItMeans(string? uiLang) => Pick(uiLang,
        "Wat dit betekent",
        "What this means",
        "Co to znaczy",
        "Ce înseamnă",
        "ماذا يعني هذا");

    /// <summary>nl / en / pl / ro / ar. Unknown languages fall back to Dutch.</summary>
    public static string Normalize(string? uiLang)
    {
        var raw = uiLang?.Trim().ToLowerInvariant() ?? "";
        if (raw.StartsWith("en", StringComparison.Ordinal)) return "en";
        if (raw.StartsWith("pl", StringComparison.Ordinal)) return "pl";
        if (raw.StartsWith("ro", StringComparison.Ordinal)) return "ro";
        if (raw.StartsWith("ar", StringComparison.Ordinal)) return "ar";
        return "nl";
    }

    private static string Pick(string? uiLang, string nl, string en, string pl, string ro, string ar)
        => Normalize(uiLang) switch
        {
            "en" => en,
            "pl" => pl,
            "ro" => ro,
            "ar" => ar,
            _ => nl
        };
}
