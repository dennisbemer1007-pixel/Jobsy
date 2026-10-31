namespace Jobsy.Infrastructure.Scholen.Passport;

/// <summary>Colored bijlagen mockup icons (HTML uses emoji; QuestPDF gets small SVG glyphs).</summary>
internal static class PupilPassportG78BijlagenVisuals
{
    internal static string RiasecGlyph(string letter) => letter.ToUpperInvariant() switch
    {
        "R" => ColoredGlyph("#c9a227", """<path d="M14 4l-2 8h3l-1 8h-4l2-10H9l1-6h4z"/>"""),
        "I" => ColoredGlyph("#0f2d5c", """<circle cx="12" cy="11" r="5"/><path d="M18 18l-4-4"/>"""),
        "A" => ColoredGlyph("#c4552a", """<path d="M8 20h8l-4-14zM10 14h4"/>"""),
        "S" => ColoredGlyph("#df5530", """<path d="M12 20s-6-3-6-8a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 5-6 8-6 8z"/>"""),
        "E" => ColoredGlyph("#0f2d5c", """<path d="M6 8h12v2H8v3h9v2H8v3h10v2H6z"/>"""),
        "C" => ColoredGlyph("#33505f", """<rect x="6" y="6" width="12" height="12" rx="1"/><path d="M9 10h6M9 14h6"/>"""),
        _ => ColoredGlyph("#0f2d5c", """<circle cx="12" cy="12" r="6"/>""")
    };

    internal static string SubjectGlyph(string subjectKey) => subjectKey.ToLowerInvariant() switch
    {
        "natuur-techniek" => JobEmoji("#0f2d5c", "microscope"),
        "rekenen" => JobEmoji("#c9a227", "calc"),
        "gym" => JobEmoji("#4f8a5f", "ball"),
        "geschiedenis" => JobEmoji("#8a6b4a", "castle"),
        "tekenen" or "handvaardigheid" => JobEmoji("#c4552a", "palette"),
        "muziek" => JobEmoji("#7b5ea7", "note"),
        "spelling" or "schrijven" => JobEmoji("#0f2d5c", "pencil"),
        _ => JobEmoji("#6b5d50", "star")
    };

    internal static string JobEmojiGlyph(string jobKey) => jobKey.ToLowerInvariant() switch
    {
        "monteur" or "elektricien" or "installatiemonteur" => JobEmoji("#c9a227", "wrench"),
        "boswachter" or "hovenier" => JobEmoji("#4f8a5f", "tree"),
        "laborant" or "bioloog" => JobEmoji("#0f2d5c", "flask"),
        "dierenverzorger" or "dierenarts" => JobEmoji("#df5530", "paw"),
        _ => JobEmoji("#c9a227", "spark")
    };

    private static string JobEmoji(string color, string kind) => kind switch
    {
        "wrench" => ColoredGlyph(color, """<path d="M14.7 6.3a4 4 0 0 0-5.4 5.4L4 17v3h3l5.3-5.3a4 4 0 0 0 5.4-5.4l-2.1 2.1-2.8-2.8Z"/>"""),
        "tree" => ColoredGlyph(color, """<path d="M12 3v4M8 10h8M10 10c0-3 2-5 2-8 0 3 2 5 2 8M12 14v7"/>"""),
        "flask" => ColoredGlyph(color, """<path d="M9 3h6v6l4 11H5L9 9V3z"/>"""),
        "paw" => ColoredGlyph(color, """<circle cx="11" cy="14" r="2"/><circle cx="7" cy="10" r="1.5"/><circle cx="11" cy="8" r="1.5"/><circle cx="15" cy="10" r="1.5"/>"""),
        "spark" => ColoredGlyph(color, """<path d="M12 2v4M12 18v4M4.9 4.9l2.8 2.8M16.3 16.3l2.8 2.8M2 12h4M18 12h4"/>"""),
        "microscope" => ColoredGlyph(color, """<path d="M6 18h12M9 18V9a3 3 0 0 1 6 0v9M12 9V5"/>"""),
        "calc" => ColoredGlyph(color, """<rect x="7" y="5" width="10" height="14" rx="1"/><path d="M9 9h6M9 12h6M9 15h3"/>"""),
        "ball" => ColoredGlyph(color, """<circle cx="12" cy="12" r="8"/><path d="M4 12h16M12 4a12 12 0 0 1 0 16"/>"""),
        "castle" => ColoredGlyph(color, """<path d="M5 20V10l3-3 4 3 4-3 3 3v10z"/>"""),
        "palette" => ColoredGlyph(color, """<path d="M12 3c-4 0-7 3-7 7 0 3 2 5 4 5h1a2 2 0 0 0 2-2c0-1-1-2-2-2h-.5c-2 0-3.5-1.5-3.5-3.5C6 6 8 4 12 4z"/>"""),
        "note" => ColoredGlyph(color, """<path d="M9 18V5l9-2v13"/><circle cx="12" cy="14" r="2" fill="currentColor"/>"""),
        "pencil" => ColoredGlyph(color, """<path d="M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/>"""),
        "star" => ColoredGlyph(color, """<path d="m12 3 2.6 5.6 6.1.7-4.5 4.2 1.2 6L12 16.6 6.6 19.5l1.2-6L3.3 9.3l6.1-.7Z" fill="currentColor" stroke="none"/>"""),
        _ => ColoredGlyph(color, """<circle cx="12" cy="12" r="6"/>""")
    };

    internal static string MirrorGradientBackground()
        => """<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="100%" preserveAspectRatio="none"><defs><linearGradient id="mg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#0f2d5c"/><stop offset="100%" stop-color="#1b4a7a"/></linearGradient></defs><rect width="100%" height="100%" fill="url(#mg)" rx="4"/></svg>""";

    internal static string StepDashLine()
        => """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 4" preserveAspectRatio="none"><line x1="0" y1="2" x2="100" y2="2" stroke="#c9a227" stroke-width="2" stroke-dasharray="4 3"/></svg>""";

    /// <summary>Soft cover decoration: light wave and bubbles (professional, low contrast).</summary>
    internal static string CoverSparkleDecor()
        => """
           <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 120" preserveAspectRatio="none">
             <path d="M0 72 Q100 48 200 68 T400 58 L400 120 L0 120 Z" fill="#fff8e6" opacity="0.85"/>
             <path d="M0 88 Q120 64 240 82 T400 74 L400 120 L0 120 Z" fill="#fffcfa" opacity="0.9"/>
             <circle cx="320" cy="28" r="14" fill="#c9a227" opacity="0.12"/>
             <circle cx="350" cy="52" r="9" fill="#c9a227" opacity="0.1"/>
             <circle cx="280" cy="44" r="6" fill="#0f2d5c" opacity="0.06"/>
             <circle cx="365" cy="18" r="5" fill="#c4552a" opacity="0.08"/>
           </svg>
           """;

    private static string ColoredGlyph(string color, string pathBody)
        => $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="{color}" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">{pathBody}</svg>""";
}
