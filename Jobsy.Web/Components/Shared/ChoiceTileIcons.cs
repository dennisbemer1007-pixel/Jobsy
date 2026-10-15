namespace Jobsy.Web.Components.Shared;

/// <summary>Inline SVG icons for shared choice tiles (16×16, stroke, no emoji).</summary>
public static class ChoiceTileIcons
{
    private const string SvgAttrs =
        "viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    public const string Plus =
        $"<svg {SvgAttrs}><path d=\"M12 5v14M5 12h14\"/></svg>";

    public const string Check =
        $"<svg {SvgAttrs}><path d=\"M5 12.5 9.5 17 19 7\"/></svg>";

    public const string Clock =
        $"<svg {SvgAttrs}><circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M12 8v4l3 2\"/></svg>";

    public const string Calendar =
        $"<svg {SvgAttrs}><rect x=\"4\" y=\"5\" width=\"16\" height=\"16\" rx=\"2\"/><path d=\"M8 3v4M16 3v4M4 11h16\"/></svg>";

    public static string? ForDislike(string? code) => Canonical(code) switch
    {
        "night-shifts" => Moon,
        "heavy-lifting" => Dumbbell,
        "working-alone" => Person,
        "phone-customers" => Phone,
        "noise" => Speaker,
        "cold-outdoor" => Snowflake,
        "computer-work" => Monitor,
        "changing-hours" => Clock,
        "crowded" => People,
        "long-travel" => MapPin,
        _ => null
    };

    public static string? ForHobby(string? code) => Canonical(code) switch
    {
        "sport" => Dumbbell,
        "music" => Music,
        "cooking" => Pot,
        "gaming" => Gamepad,
        "crafts" => Scissors,
        "nature" => Leaf,
        "reading" => Book,
        "caring" => Heart,
        "tech" => Monitor,
        "art" => Palette,
        "volunteering" => Hand,
        "fashion" => Shirt,
        _ => null
    };

    public static string? ForEmployer(string? code) => Canonical(code) switch
    {
        "small-team" => People,
        "large-company" => Building,
        "fixed-workplace" => MapPin,
        "learn-on-the-job" => Book,
        "dutch-support" => Chat,
        "growth" => TrendUp,
        "close-to-home" => Home,
        "variety" => Layers,
        _ => null
    };

    public static string? ForWorkStatus(string? code) => Canonical(code) switch
    {
        "scholier" or "student" or "stagiair" => Graduation,
        "loondienst-parttime" or "loondienst-fulltime" => Briefcase,
        "flex" => Clock,
        "zzp" or "ondernemer" => Briefcase,
        "zoekend" => Search,
        "herintreder" => TrendUp,
        "gepensioneerd-werkt" => Home,
        _ => null
    };

    public static string? ForTransport(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var c = code.Trim().ToLowerInvariant();
        return c switch
        {
            "fiets" or "bike" => Bike,
            "e-bike" or "ebike" => Bike,
            "auto" or "car" => Car,
            "ov" or "publictransport" or "transit" => Bus,
            "lopend" or "walking" or "walk" => Walk,
            _ => null
        };
    }

    public static string? ForAvailabilityPreset(string? code) => Canonical(code) switch
    {
        "school" => Book,
        "weekend" => Calendar,
        "evening" => Moon,
        "office" => Building,
        "holiday" => Sun,
        "parttime" => Clock,
        "fulltime" => Briefcase,
        "custom" => Plus,
        _ => Calendar
    };

    private static string? Canonical(string? code)
        => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToLowerInvariant();

    private const string Moon = $"<svg {SvgAttrs}><path d=\"M21 14.5A8.5 8.5 0 0 1 9.5 3 7 7 0 1 0 21 14.5z\"/></svg>";
    private const string Dumbbell = $"<svg {SvgAttrs}><path d=\"M6 9v6M18 9v6M4 10h4M16 10h4M8 12h8\"/></svg>";
    private const string Person = $"<svg {SvgAttrs}><circle cx=\"12\" cy=\"8\" r=\"3\"/><path d=\"M6 20v-1a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v1\"/></svg>";
    private const string Phone = $"<svg {SvgAttrs}><path d=\"M6.5 4.5 9 3l2 4.5-1.5 1a11 11 0 0 0 5 5l1-1.5L20 14l-1.5 2.5a2 2 0 0 1-2.2.9 15 15 0 0 1-8.8-8.8 2 2 0 0 1 .9-2.2z\"/></svg>";
    private const string Speaker = $"<svg {SvgAttrs}><path d=\"M11 5 6 9H3v6h3l5 4V5z\"/><path d=\"M16 9a4 4 0 0 1 0 6M18.5 6.5a7 7 0 0 1 0 11\"/></svg>";
    private const string Snowflake = $"<svg {SvgAttrs}><path d=\"M12 2v20M4 6l16 12M20 6 4 18M2 12h20\"/></svg>";
    private const string Monitor = $"<svg {SvgAttrs}><rect x=\"3\" y=\"5\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 21h8M12 17v4\"/></svg>";
    private const string People = $"<svg {SvgAttrs}><path d=\"M16 21v-2a3 3 0 0 0-3-3H7a3 3 0 0 0-3 3v2\"/><circle cx=\"10\" cy=\"8\" r=\"3\"/><path d=\"M22 21v-2a3 3 0 0 0-2-2.8\"/><path d=\"M16 4.2a3 3 0 0 1 0 5.6\"/></svg>";
    private const string MapPin = $"<svg {SvgAttrs}><path d=\"M12 21s6-5.2 6-10a6 6 0 1 0-12 0c0 4.8 6 10 6 10z\"/><circle cx=\"12\" cy=\"11\" r=\"2.5\"/></svg>";
    private const string Music = $"<svg {SvgAttrs}><path d=\"M9 18V6l10-2v12\"/><circle cx=\"7\" cy=\"18\" r=\"2\"/><circle cx=\"17\" cy=\"16\" r=\"2\"/></svg>";
    private const string Pot = $"<svg {SvgAttrs}><path d=\"M4 10h16v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-8z\"/><path d=\"M8 10V7a4 4 0 0 1 8 0v3\"/></svg>";
    private const string Gamepad = $"<svg {SvgAttrs}><rect x=\"4\" y=\"8\" width=\"16\" height=\"10\" rx=\"3\"/><path d=\"M9 12v4M7 14h4M15 13h.01M17 15h.01\"/></svg>";
    private const string Scissors = $"<svg {SvgAttrs}><circle cx=\"7\" cy=\"7\" r=\"2\"/><circle cx=\"7\" cy=\"17\" r=\"2\"/><path d=\"M20 4 9.5 15M20 20 9.5 9\"/></svg>";
    private const string Leaf = $"<svg {SvgAttrs}><path d=\"M6 20c6-1 10-5 12-12-7 2-11 6-12 12z\"/><path d=\"M6 20c2-4 6-8 12-10\"/></svg>";
    private const string Book = $"<svg {SvgAttrs}><path d=\"M5 5h7a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3V5z\"/><path d=\"M19 5h-7a3 3 0 0 0-3 3v13h7a3 3 0 0 0 3-3V5z\"/></svg>";
    private const string Heart = $"<svg {SvgAttrs}><path d=\"M12 20s-6.5-4.5-8.5-8.5C1.5 8.5 4 5 7.5 5 10 5 12 7 12 7s2-2 4.5-2C20 5 22.5 8.5 20.5 11.5 18.5 15.5 12 20 12 20z\"/></svg>";
    private const string Palette = $"<svg {SvgAttrs}><circle cx=\"12\" cy=\"12\" r=\"8\"/><circle cx=\"9\" cy=\"10\" r=\"1\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"15\" cy=\"10\" r=\"1\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"10\" cy=\"15\" r=\"1\" fill=\"currentColor\" stroke=\"none\"/></svg>";
    private const string Hand = $"<svg {SvgAttrs}><path d=\"M7 11V6a2 2 0 1 1 4 0v5\"/><path d=\"M11 11V5a2 2 0 1 1 4 0v7\"/><path d=\"M15 12V7a2 2 0 1 1 4 0v8a5 5 0 0 1-5 5H9a4 4 0 0 1-4-4v-1a2 2 0 1 1 4 0\"/></svg>";
    private const string Shirt = $"<svg {SvgAttrs}><path d=\"M8 4 5 8v12h14V8l-3-4\"/><path d=\"M8 4h8l2 3H6l2-3z\"/><path d=\"M12 4v4\"/></svg>";
    private const string Building = $"<svg {SvgAttrs}><path d=\"M4 21V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v16\"/><path d=\"M8 21v-4h2v4M14 21v-4h2v4M8 9h.01M12 9h.01M16 9h.01M8 13h.01M12 13h.01M16 13h.01\"/></svg>";
    private const string Chat = $"<svg {SvgAttrs}><path d=\"M21 12a7 7 0 0 1-7 7H8l-4 3V12a7 7 0 1 1 14 0z\"/></svg>";
    private const string TrendUp = $"<svg {SvgAttrs}><path d=\"M4 16 9 11l4 4 7-7\"/><path d=\"M15 8h5v5\"/></svg>";
    private const string Home = $"<svg {SvgAttrs}><path d=\"M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5z\"/></svg>";
    private const string Layers = $"<svg {SvgAttrs}><path d=\"M12 3 3 8l9 5 9-5-9-5z\"/><path d=\"m3 12 9 5 9-5\"/><path d=\"m3 17 9 5 9-5\"/></svg>";
    private const string Graduation = $"<svg {SvgAttrs}><path d=\"M3 9 12 4l9 5-9 5-9-5z\"/><path d=\"M21 10v6\"/><path d=\"M6 13v4c0 2 3 3 6 3s6-1 6-3v-4\"/></svg>";
    private const string Briefcase = $"<svg {SvgAttrs}><rect x=\"3\" y=\"8\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 8V6a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2\"/></svg>";
    private const string Search = $"<svg {SvgAttrs}><circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20 20-3.5-3.5\"/></svg>";
    private const string Bike = $"<svg {SvgAttrs}><circle cx=\"6\" cy=\"17\" r=\"3\"/><circle cx=\"18\" cy=\"17\" r=\"3\"/><path d=\"M6 17h5l3-7h4l2 3\"/><path d=\"M9 10 6 17\"/></svg>";
    private const string Car = $"<svg {SvgAttrs}><path d=\"M5 11l1.5-4h13L21 11\"/><path d=\"M5 11v6a1 1 0 0 0 1 1h1a1 1 0 0 0 1-1v-1h10v1a1 1 0 0 0 1 1h1a1 1 0 0 0 1-1v-6\"/><circle cx=\"8\" cy=\"15\" r=\"1\"/><circle cx=\"16\" cy=\"15\" r=\"1\"/></svg>";
    private const string Bus = $"<svg {SvgAttrs}><rect x=\"4\" y=\"6\" width=\"16\" height=\"12\" rx=\"2\"/><path d=\"M4 12h16M8 18v2M16 18v2M8 10h.01M16 10h.01\"/></svg>";
    private const string Walk = $"<svg {SvgAttrs}><circle cx=\"12\" cy=\"5\" r=\"2\"/><path d=\"M10 22V12l-2-2 2-4h4l2 4-2 2v10\"/></svg>";
    private const string Sun = $"<svg {SvgAttrs}><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\"/></svg>";
}
