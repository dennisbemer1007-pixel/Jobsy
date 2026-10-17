namespace Jobsy.Infrastructure.Scholen.Passport;

/// <summary>Embedded SVG icons for pupil passports (no emoji — QuestPDF fonts lack color emoji glyphs).</summary>
internal static class PupilPassportIcons
{
    private const string SvgOpen = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">""";
    private const string SvgClose = "</svg>";

    internal static string Icon(string id, string colorHex, float sizePt = 12)
    {
        var body = id switch
        {
            "check" => """<path d="M20 6 9 17l-5-5"/>""",
            "bullet" => """<circle cx="12" cy="12" r="4" fill="currentColor" stroke="none"/>""",
            "checkbox" => """<rect x="4" y="4" width="16" height="16" rx="3"/><path d="M8 12l3 3 5-6"/>""",
            "checkbox-empty" => """<rect x="4" y="4" width="16" height="16" rx="3"/>""",
            "star" => """<path d="m12 3 2.6 5.6 6.1.7-4.5 4.2 1.2 6L12 16.6 6.6 19.5l1.2-6L3.3 9.3l6.1-.7Z" fill="currentColor" stroke="none"/>""",
            "arrow" => """<path d="M5 12h14M13 6l6 6-6 6"/>""",
            "heart" => """<path d="M19 14c1.5-1.5 3-3.2 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.8 0-3 .5-4.5 2-1.5-1.5-2.7-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4 3 5.5l7 7Z" fill="currentColor" stroke="none"/>""",
            "dna" => """<path d="M7 3c0 6 10 6 10 12s-10 6-10 6M17 3c0 6-10 6-10 12s10 6 10 6"/><path d="M8 7h8M8 17h8"/>""",
            "flag" => """<path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/><path d="M4 22v-7"/>""",
            "bulb" => """<path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"/><path d="M9 18h6M10 22h4"/>""",
            "shield" => """<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/>""",
            "quote" => """<path d="M3 21c3 0 7-1 7-8V5H3v8h4c0 4-2 5-4 5zM14 21c3 0 7-1 7-8V5h-7v8h4c0 4-2 5-4 5z" fill="currentColor" stroke="none"/>""",
            "users" => """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8"/>""",
            "clock" => """<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>""",
            "cap" => """<path d="M22 10 12 5 2 10l10 5 10-5Z"/><path d="M6 12v5c3 3 9 3 12 0v-5"/>""",
            "pin" => """<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>""",
            "brief" => """<rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>""",
            "lock" => """<rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>""",
            "paw" => """<circle cx="11" cy="14" r="2.5" fill="currentColor" stroke="none"/><circle cx="7" cy="10" r="1.8" fill="currentColor" stroke="none"/><circle cx="11" cy="8" r="1.8" fill="currentColor" stroke="none"/><circle cx="15" cy="10" r="1.8" fill="currentColor" stroke="none"/><path d="M6 17c1.5-2 4-2.5 5-2.5s3.5.5 5 2.5"/>""",
            "hands" => """<path d="M8 12V8a2 2 0 1 1 4 0v4M12 12V6a2 2 0 1 1 4 0v6M16 12V9a2 2 0 1 1 4 0v8a4 4 0 0 1-4 4H9a5 5 0 0 1-5-5v-2a2 2 0 1 1 4 0"/>""",
            "pencil" => """<path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/>""",
            "search" => """<circle cx="11" cy="11" r="7"/><path d="M20 20l-3-3"/>""",
            "wrench" => """<path d="M14.7 6.3a4 4 0 0 0-5.4 5.4L4 17v3h3l5.3-5.3a4 4 0 0 0 5.4-5.4l-2.1 2.1-2.8-2.8Z"/>""",
            "target" => """<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5" fill="currentColor" stroke="none"/>""",
            "spark" => """<path d="M12 2v4M12 18v4M4.9 4.9l2.8 2.8M16.3 16.3l2.8 2.8M2 12h4M18 12h4M4.9 19.1l2.8-2.8M16.3 7.7l2.8-2.8"/>""",
            "giraffe" => """<path d="M9 20v-6M15 20v-6M12 6c-2 0-3 2-3 4v2h6V10c0-2-1-4-3-4Z"/><path d="M12 4V2"/>""",
            "microscope" => """<path d="M6 18h12M9 18V9a3 3 0 0 1 6 0v9M12 9V5"/>""",
            "flower" => """<circle cx="12" cy="12" r="2" fill="currentColor" stroke="none"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1l2.1-2.1M17 7l2.1-2.1"/>""",
            "world-coral" => """<path d="M12 3c-4 0-7 3-7 7 0 5 7 11 7 11s7-6 7-11c0-4-3-7-7-7Z"/><circle cx="12" cy="10" r="2.5"/>""",
            "world-gem" => """<path d="M12 2 4 8l8 14 8-14Z"/><path d="M4 8h16"/>""",
            "world-lamp" => """<path d="M9 18h6M12 2v4M8 10a4 4 0 0 1 8 0c0 2-1 3-2 4H10c-1-1-2-2-2-4Z"/>""",
            "world-island" => """<path d="M3 18c3-4 15-4 18 0"/><path d="M8 14c1-2 3-3 4-3s3 1 4 3"/><circle cx="16" cy="8" r="2"/>""",
            _ => """<circle cx="12" cy="12" r="8"/>"""
        };

        _ = sizePt;
        var svg = $"{SvgOpen}{body}{SvgClose}";
        return svg.Replace("currentColor", colorHex, StringComparison.Ordinal);
    }

    internal static string RoutePathSvg(string strokeHex)
        => $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 96" preserveAspectRatio="none"><path d="M20 21 C 80 21, 90 59, 140 59 S 210 21, 255 21 S 330 59, 374 56" fill="none" stroke="{strokeHex}" stroke-width="4" stroke-dasharray="2 9" stroke-linecap="round"/></svg>""";

    internal static string GridPaperBackground(string dotHex)
        => $"""<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%"><defs><pattern id="g" width="18" height="18" patternUnits="userSpaceOnUse"><circle cx="1.2" cy="1.2" r="1.2" fill="{dotHex}"/></pattern></defs><rect width="100%" height="100%" fill="#ffffff"/><rect width="100%" height="100%" fill="url(#g)"/></svg>""";

    internal static string StrengthIconKey(string title) => title switch
    {
        "Resultaatgericht" => "target",
        "Rustig onder druk" => "bulb",
        "Nieuwe ideeën" => "spark",
        "In contact komen" => "users",
        "Samenwerken" => "hands",
        "Zorgvuldig werken" => "pencil",
        "Anderen helpen" => "heart",
        "Uitzoeken" => "search",
        "Praktisch aanpakken" => "wrench",
        "Samen iets doen" => "users",
        "Doelen halen" => "flag",
        "Iets betekenen" => "world-coral",
        "Betrouwbaar" => "shield",
        _ => "star"
    };

    internal static string JobCardIconKey(string jobKey) => jobKey switch
    {
        "dierenverzorger" or "dierenarts" => "giraffe",
        "bioloog" or "wetenschapper" => "microscope",
        "hovenier" => "flower",
        _ => "brief"
    };

    internal static string WorldIconKey(int worldNumber) => worldNumber switch
    {
        1 => "world-coral",
        2 => "world-gem",
        3 => "world-lamp",
        4 => "world-island",
        _ => "star"
    };

    internal static string DnaLayerIcon(int index) => index switch
    {
        0 => "heart",
        1 => "dna",
        2 => "flag",
        _ => "shield"
    };
}
