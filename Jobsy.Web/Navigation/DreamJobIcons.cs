namespace Jobsy.Web.Navigation;

// Paths are adapted from Lucide Icons (ISC License): https://lucide.dev/
// Each SVG deliberately inherits currentColor so the tile controls its contrast.
public static class DreamJobIcons
{
    private const string SvgStart = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\">";
    private const string SvgEnd = "</svg>";

    public const string PawPrint = SvgStart + "<circle cx=\"11\" cy=\"4\" r=\"2\"/><circle cx=\"18\" cy=\"8\" r=\"2\"/><circle cx=\"4\" cy=\"8\" r=\"2\"/><path d=\"M9 10c-2 1.5-4 4-4 6a4 4 0 0 0 8 0c0-2-2-4.5-4-6Z\"/>" + SvgEnd;
    public const string Plane = SvgStart + "<path d=\"M17.8 19.2 16 11l4.5-4.5a1.5 1.5 0 0 0-2.1-2.1L14 9l-8.2-1.8-2 2 6 2.8-3.2 3.2-2.1-.4-1.5 1.5 4.1 1.2 1.2 4.1 1.5-1.5-.4-2.1 3.2-3.2 2.8 6 2-2Z\"/>" + SvgEnd;
    public const string Scale = SvgStart + "<path d=\"M16 16 3 3M8 21l3-3 3 3M14 3h7M6 7h12M18 7a3 3 0 1 1-6 0l3-4 3 4ZM9 7a3 3 0 1 1-6 0l3-4 3 4Z\"/>" + SvgEnd;
    public const string Presentation = SvgStart + "<path d=\"M2 3h20v14H2zM12 17v4M8 21h8M7 8h2M15 8h2M10 12h4\"/>" + SvgEnd;
    public const string Stethoscope = SvgStart + "<path d=\"M4 2v6a4 4 0 0 0 8 0V2M8 2v3M12 2v3M19 10v2a5 5 0 0 1-5 5h-2a4 4 0 0 0-4 4v1M19 10a2 2 0 1 1 0 4 2 2 0 0 1 0-4Z\"/>" + SvgEnd;
    public const string DraftingCompass = SvgStart + "<circle cx=\"12\" cy=\"5\" r=\"2\"/><path d=\"m3 21 6-14M21 21 15 7M5 16h14M12 7v14\"/>" + SvgEnd;
    public const string ChefHat = SvgStart + "<path d=\"M6 15a4 4 0 1 1 2-7.5A5 5 0 0 1 17.5 8 4 4 0 1 1 18 15ZM6 15v4h12v-4\"/>" + SvgEnd;
    public const string Flame = SvgStart + "<path d=\"M12 22c4 0 7-2.7 7-6.5 0-3-2-5-4-7-1 3-3 4-4 5-1-2-1-4 .5-6.5C7 9.5 5 13 5 15.5 5 19.3 8 22 12 22Z\"/>" + SvgEnd;
    public const string Gamepad2 = SvgStart + "<path d=\"M6 12h4M8 10v4M15 13h.01M18 11h.01\"/><path d=\"M5 7h14a3 3 0 0 1 3 3v5a3 3 0 0 1-3 3h-2l-2-2H9l-2 2H5a3 3 0 0 1-3-3v-5a3 3 0 0 1 3-3Z\"/>" + SvgEnd;
    public const string Rocket = SvgStart + "<path d=\"M4.5 16.5c-1.5 1-2 3-2 3s2-.5 3-2l2-2M9 15l-3-3a16 16 0 0 1 10-9l2 2a16 16 0 0 1-9 10ZM13 9h.01\"/>" + SvgEnd;
    public const string Shield = SvgStart + "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z\"/>" + SvgEnd;
    public const string HeartPulse = SvgStart + "<path d=\"M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z\"/><path d=\"M3.5 12h4l1.5-3 3 6 1.5-3h4\"/>" + SvgEnd;
    public const string Briefcase = SvgStart + "<rect x=\"3\" y=\"7\" width=\"18\" height=\"13\" rx=\"2\"/><path d=\"M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18M10 12v2h4v-2\"/>" + SvgEnd;
    public const string Mic = SvgStart + "<rect x=\"9\" y=\"2\" width=\"6\" height=\"12\" rx=\"3\"/><path d=\"M5 10a7 7 0 0 0 14 0M12 17v4M8 21h8\"/>" + SvgEnd;
    public const string Camera = SvgStart + "<path d=\"M14.5 4 16 7h4a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h4l1.5-3Z\"/><circle cx=\"12\" cy=\"13\" r=\"3\"/>" + SvgEnd;
    public const string Scissors = SvgStart + "<circle cx=\"6\" cy=\"6\" r=\"3\"/><circle cx=\"6\" cy=\"18\" r=\"3\"/><path d=\"m8.6 7.5 10.9 6.3M8.6 16.5l10.9-6.3\"/>" + SvgEnd;
    public const string Code = SvgStart + "<path d=\"m8 9-3 3 3 3M16 9l3 3-3 3M14 5l-4 14\"/>" + SvgEnd;
    public const string HardHat = SvgStart + "<path d=\"M2 18h20M4 18v-5a8 8 0 0 1 16 0v5M12 5v4M6 13h12\"/>" + SvgEnd;
    public const string Smile = SvgStart + "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M8 14s1.5 2 4 2 4-2 4-2M9 9h.01M15 9h.01\"/>" + SvgEnd;
    public const string Brain = SvgStart + "<path d=\"M9.5 4a3 3 0 0 0-5 2.2A3 3 0 0 0 5 12a3 3 0 0 0 4.5 3v3a3 3 0 0 0 5 0v-3A3 3 0 0 0 19 12a3 3 0 0 0 .5-5.8A3 3 0 0 0 14.5 4c-1 0-1.8.5-2.5 1.2A3.2 3.2 0 0 0 9.5 4Z\"/><path d=\"M12 5v14M9 9H6M15 9h3\"/>" + SvgEnd;
    public const string Activity = SvgStart + "<path d=\"M3 12h4l2-6 4 12 2-6h6\"/>" + SvgEnd;
    public const string Baby = SvgStart + "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M9 10h.01M15 10h.01M9 15s1 1 3 1 3-1 3-1M12 3c0 2 2 2 2 4\"/>" + SvgEnd;
    public const string Pill = SvgStart + "<path d=\"m10.5 20.5-7-7a4 4 0 0 1 5.7-5.7l7 7a4 4 0 0 1-5.7 5.7Z\"/><path d=\"m8 5 11 11\"/>" + SvgEnd;
    public const string Gavel = SvgStart + "<path d=\"m14 13-8-8 3-3 8 8M14 13l3-3 3 3-3 3M14 13l-7 7M3 21h12\"/>" + SvgEnd;
    public const string Stamp = SvgStart + "<path d=\"M5 22h14M7 18h10M8 18v-3a4 4 0 0 1 8 0v3M10 11V6a2 2 0 1 1 4 0v5\"/>" + SvgEnd;
    public const string Cog = SvgStart + "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4\"/>" + SvgEnd;
    public const string PlugZap = SvgStart + "<path d=\"M9 7V3M15 7V3M6 7h12v4a6 6 0 0 1-12 0V7ZM12 17v4M19 14l-3 4h3l-3 4\"/>" + SvgEnd;
    public const string Wrench = SvgStart + "<path d=\"M14.7 6.3a4 4 0 0 0-5 5L3 18a2 2 0 1 0 3 3l6.7-6.7a4 4 0 0 0 5-5l-3 3-3-3 3-3Z\"/>" + SvgEnd;
    public const string Hammer = SvgStart + "<path d=\"m15 12-9 9M14 4l6 6-4 4-6-6 4-4ZM5 5l4 4\"/>" + SvgEnd;
    public const string Trees = SvgStart + "<path d=\"m10 13-3-3 3-6 3 6-3 3ZM10 13v7M18 16l-3-3 3-6 3 6-3 3ZM18 16v4M3 21h18\"/>" + SvgEnd;
    public const string Leaf = SvgStart + "<path d=\"M11 20A7 7 0 0 1 4 13C4 7 10 4 20 4c0 10-3 16-9 16ZM4 17c3-3 6-5 11-7\"/>" + SvgEnd;
    public const string FlaskConical = SvgStart + "<path d=\"M10 2v7l-6 10a2 2 0 0 0 2 3h12a2 2 0 0 0 2-3l-6-10V2M8 2h8M7 16h10\"/>" + SvgEnd;
    public const string PenTool = SvgStart + "<path d=\"m12 19 7-7-4-4-7 7-4 1 1-4 7-7 4 4M14 3l7 7M3 21h6\"/>" + SvgEnd;
    public const string Music = SvgStart + "<path d=\"M9 18V5l10-2v13M9 18a3 3 0 1 1-3-3 3 3 0 0 1 3 3ZM19 16a3 3 0 1 1-3-3 3 3 0 0 1 3 3Z\"/>" + SvgEnd;
    public const string Drama = SvgStart + "<path d=\"M4 11a8 8 0 0 0 16 0V5H4v6ZM8 9h.01M16 9h.01M8 14s1 1 4 1 4-1 4-1\"/>" + SvgEnd;
    public const string Trophy = SvgStart + "<path d=\"M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0V4ZM7 6H3v1a5 5 0 0 0 4 4M17 6h4v1a5 5 0 0 1-4 4\"/>" + SvgEnd;
    public const string Dumbbell = SvgStart + "<path d=\"m6.5 6.5 11 11M3 9v6M6 6v12M18 6v12M21 9v6\"/>" + SvgEnd;
    public const string Video = SvgStart + "<rect x=\"3\" y=\"5\" width=\"13\" height=\"14\" rx=\"2\"/><path d=\"m16 10 5-3v10l-5-3\"/>" + SvgEnd;
    public const string Megaphone = SvgStart + "<path d=\"m3 11 15-6v14L3 13v-2ZM18 9h2a2 2 0 0 1 0 4h-2M6 14l1 5h3l-1-4\"/>" + SvgEnd;
    public const string Calculator = SvgStart + "<rect x=\"5\" y=\"2\" width=\"14\" height=\"20\" rx=\"2\"/><path d=\"M8 6h8M8 11h.01M12 11h.01M16 11h.01M8 15h.01M12 15h.01M16 15h.01M8 19h.01M12 19h.01M16 19h.01\"/>" + SvgEnd;
    public const string House = SvgStart + "<path d=\"m3 10 9-7 9 7v11H3V10ZM9 21v-6h6v6\"/>" + SvgEnd;
    public const string Luggage = SvgStart + "<rect x=\"5\" y=\"6\" width=\"14\" height=\"15\" rx=\"2\"/><path d=\"M9 6V4h6v2M5 12h14M9 16h.01M15 16h.01\"/>" + SvgEnd;
    public const string Medal = SvgStart + "<path d=\"m8 3 4 5 4-5M12 8a6 6 0 1 1 0 12 6 6 0 0 1 0-12ZM12 11v4M10 13h4\"/>" + SvgEnd;
    public const string Blocks = SvgStart + "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/>" + SvgEnd;
    public const string Rabbit = SvgStart + "<path d=\"M13 9c2-4 4-6 6-5 2 2-1 6-3 8a6 6 0 1 1-10 0c-2-2-5-6-3-8 2-1 4 1 6 5M9 14h.01M15 14h.01\"/>" + SvgEnd;
    public const string PartyPopper = SvgStart + "<path d=\"M5 21 14 12M7 17l-3-3 4-4 3 3M14 12l4-4M16 4h.01M20 6h.01M18 10h.01\"/>" + SvgEnd;
    public const string ChartLine = SvgStart + "<path d=\"M3 3v18h18M7 16l4-5 3 3 5-7\"/>" + SvgEnd;
    public const string Croissant = SvgStart + "<path d=\"M4 15a8 8 0 0 1 13-8 8 8 0 0 1 3 11 8 8 0 0 1-16-3ZM8 8c2 2 4 5 4 9M16 8c-2 2-4 5-4 9\"/>" + SvgEnd;

    private static readonly Dictionary<string, string> Lookup = new(StringComparer.OrdinalIgnoreCase)
    {
        ["paw-print"] = PawPrint,
        ["plane"] = Plane,
        ["scale"] = Scale,
        ["presentation"] = Presentation,
        ["stethoscope"] = Stethoscope,
        ["drafting-compass"] = DraftingCompass,
        ["chef-hat"] = ChefHat,
        ["flame"] = Flame,
        ["gamepad-2"] = Gamepad2,
        ["rocket"] = Rocket,
        ["shield"] = Shield,
        ["heart-pulse"] = HeartPulse,
        ["briefcase"] = Briefcase,
        ["mic"] = Mic,
        ["camera"] = Camera,
        ["scissors"] = Scissors,
        ["code"] = Code,
        ["hard-hat"] = HardHat,
        ["smile"] = Smile,
        ["brain"] = Brain,
        ["activity"] = Activity,
        ["baby"] = Baby,
        ["pill"] = Pill,
        ["gavel"] = Gavel,
        ["stamp"] = Stamp,
        ["cog"] = Cog,
        ["plug-zap"] = PlugZap,
        ["wrench"] = Wrench,
        ["hammer"] = Hammer,
        ["trees"] = Trees,
        ["leaf"] = Leaf,
        ["flask-conical"] = FlaskConical,
        ["pen-tool"] = PenTool,
        ["music"] = Music,
        ["drama"] = Drama,
        ["trophy"] = Trophy,
        ["dumbbell"] = Dumbbell,
        ["video"] = Video,
        ["megaphone"] = Megaphone,
        ["calculator"] = Calculator,
        ["house"] = House,
        ["luggage"] = Luggage,
        ["medal"] = Medal,
        ["blocks"] = Blocks,
        ["rabbit"] = Rabbit,
        ["party-popper"] = PartyPopper,
        ["chart-line"] = ChartLine,
        ["croissant"] = Croissant
    };

    public static string? TryGet(string iconKey) => Lookup.TryGetValue(iconKey, out var svg) ? svg : null;
}
