namespace Jobsy.Web.Scholen;

/// <summary>Minimal chip icons aligned with G78 mockup <c>pchip</c> (stroke icons in circle).</summary>
internal static class PupilGroep78ChipIcons
{
    private const string WrapStart = """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private const string WrapEnd = "</svg>";

    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sport"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 3v18M3 12h18\"/>",
        ["buiten"] = "<path d=\"M12 3v4M12 17v4M4.2 7.5l3 3M16.8 16.5l3 3M3 12h4M17 12h4\"/>",
        ["dieren"] = "<circle cx=\"9\" cy=\"9\" r=\"2\"/><circle cx=\"15\" cy=\"9\" r=\"2\"/><path d=\"M7 16c1.5-2 8.5-2 10 0\"/>",
        ["gamen"] = "<rect x=\"4\" y=\"8\" width=\"16\" height=\"10\" rx=\"3\"/><path d=\"M9 12v4M7 14h4\"/>",
        ["tekenen"] = "<path d=\"M4 20l8-8 4 4 8-8\"/>",
        ["muziek"] = "<path d=\"M9 18V6l10-2v12\"/><circle cx=\"7\" cy=\"18\" r=\"2\"/><circle cx=\"17\" cy=\"16\" r=\"2\"/>",
        ["koken"] = "<path d=\"M6 10h12v8H6z\"/><path d=\"M8 10V6M12 10V5M16 10V6\"/>",
        ["fietsen-repareren"] = "<circle cx=\"7\" cy=\"17\" r=\"3\"/><circle cx=\"17\" cy=\"17\" r=\"3\"/><path d=\"M7 17l4-7h3l3 7\"/>",
        ["bouwen"] = "<path d=\"M4 20h16M7 20V9l5-4 5 4v11\"/>",
        ["techniek"] = "<path d=\"M14 7l3-3 4 4-3 3M5 20l9-9\"/>",
        ["lezen"] = "<path d=\"M5 6c2-1 4-1 7 0 3-1 5-1 7 0v12c-2-1-4-1-7 0-3-1-5-1-7 0z\"/>",
        ["dansen"] = "<path d=\"M8 20l4-8 4 2-3 6\"/>",
        ["theater"] = "<path d=\"M4 8h16M6 8l2 12h8l2-12\"/>",
        ["filmpjes"] = "<rect x=\"4\" y=\"7\" width=\"16\" height=\"12\" rx=\"2\"/><path d=\"M10 11l4 2-4 2z\"/>",
        ["mode"] = "<path d=\"M8 7l4-3 4 3v13H8z\"/>",
        ["kleine-kinderen"] = "<circle cx=\"12\" cy=\"8\" r=\"3\"/><path d=\"M6 20c1-4 10-4 12 0\"/>",
        ["natuur"] = "<path d=\"M12 3c-3 4-6 7-6 11a6 6 0 0012 0c0-4-3-7-6-11z\"/>",
        ["autos"] = "<path d=\"M5 16l2-5h10l2 5\"/><circle cx=\"8\" cy=\"17\" r=\"2\"/><circle cx=\"16\" cy=\"17\" r=\"2\"/>",
        ["computers"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 21h8\"/>",
        ["puzzels"] = "<path d=\"M8 8h3v3H8zM13 8h3v3h-3zM8 13h3v3H8zM13 13h3v3h-3z\"/>",
        ["rekenen"] = "<path d=\"M7 7h10v10H7z\"/><path d=\"M9 9l6 6M15 9l-6 6\"/>",
        ["talen"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18\"/>",
        ["reizen"] = "<path d=\"M4 16l8-10 8 10H4z\"/>",
        ["programmeren"] = "<path d=\"M8 8l-4 4 4 4M16 8l4 4-4 4M14 4l-4 16\"/>",
        ["voor-de-klas"] = "<path d=\"M12 4v16M8 8h8M8 16h8\"/>",
        ["lang-stilzitten"] = "<rect x=\"6\" y=\"5\" width=\"12\" height=\"14\" rx=\"2\"/>",
        ["hard-werken-kou"] = "<path d=\"M12 3v18M8 7h8M8 11h8\"/>",
        ["veel-lezen"] = "<path d=\"M6 6h12v14H6z\"/>",
        ["alleen-werken"] = "<circle cx=\"12\" cy=\"8\" r=\"3\"/><path d=\"M6 20c1-3 10-3 12 0\"/>",
        ["druk-lawaai"] = "<path d=\"M5 10h4l4-3v14l-4-3H5z\"/>",
        ["vies-worden"] = "<path d=\"M8 8c0 4 2 8 4 8s4-4 4-8\"/>",
        ["__other__"] = "<path d=\"M12 5v14M5 12h14\"/>",
    };

    internal static string Svg(string key)
        => WrapStart + (Paths.TryGetValue(key, out var p) ? p : Paths["sport"]) + WrapEnd;
}
