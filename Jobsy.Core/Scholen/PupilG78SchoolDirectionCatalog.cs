namespace Jobsy.Core.Scholen;

/// <summary>School directions as ideas only (not advice). NL + EN B1.</summary>
public static class PupilG78SchoolDirectionCatalog
{
    public sealed record Entry(string Key, string TitleNl, string TitleEn, string IdeaNl, string IdeaEn);

    public static readonly IReadOnlyList<Entry> All =
    [
        new("vmbo-basis", "VMBO basis", "VMBO basic",
            "Past bij veel oefenen en duidelijke stappen.",
            "Fits learning with practice and clear steps."),
        new("vmbo-kader", "VMBO kader", "VMBO middle",
            "Past bij vakken combineren met praktijk.",
            "Fits mixing subjects with hands-on work."),
        new("vmbo-techniek", "VMBO techniek", "VMBO technology",
            "Past bij maken, techniek of buiten.",
            "Fits making, technology or outdoor work."),
        new("vmbo-zorg", "VMBO zorg & welzijn", "VMBO care",
            "Past bij helpen en samenwerken.",
            "Fits helping and working together."),
        new("havo", "HAVO", "HAVO (senior general)",
            "Past bij brede vakken en doorzetten.",
            "Fits a broad range of subjects and persistence."),
        new("vwo", "VWO", "pre-university (VWO)",
            "Past bij veel zelf uitzoeken en theorie.",
            "Fits exploring topics deeply on your own."),
        new("praktijk-mbo", "Later MBO (richting idee)", "Later vocational (MBO idea)",
            "Past bij leren door doen in een vak.",
            "Fits learning a trade by doing."),
        new("gemengd-profiel", "Gemengd profiel (idee)", "Mixed profile (idea)",
            "Past als je in meerdere vakken sterke punten hebt.",
            "Fits when you are strong in several subjects."),
    ];

    public static Entry? TryGet(string key)
        => All.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
}
