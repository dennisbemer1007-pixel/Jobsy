namespace Jobsy.Web.Components.Ui;

/// <summary>24×24 line SVG icons for app chrome (stroke 1.75, no emoji).</summary>
public static class LineIcons
{
    public const string StrokeWidth = "1.75";
    private const string A =
        $"viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"{StrokeWidth}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    public const string Check = $"<svg {A}><path d=\"M5 12.5 9.5 17 19 7\"/></svg>";
    public const string Eye = $"<svg {A}><path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></svg>";
    public const string Save = $"<svg {A}><path d=\"M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2Z\"/><path d=\"M17 21v-8H7v8M7 3v5h8\"/></svg>";
    public const string Warning = $"<svg {A}><path d=\"M12 9v4M12 17h.01\"/><path d=\"M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z\"/></svg>";
    public const string Compass = $"<svg {A}><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"m16 8-2 6-6 2 2-6 6-2Z\"/></svg>";
    public const string Lock = $"<svg {A}><rect x=\"5\" y=\"11\" width=\"14\" height=\"10\" rx=\"2\"/><path d=\"M8 11V8a4 4 0 0 1 8 0v3\"/></svg>";
    public const string Target = $"<svg {A}><circle cx=\"12\" cy=\"12\" r=\"8\"/><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2M12 20v2M2 12h2M20 12h2\"/></svg>";
    public const string ClockPending = $"<svg {A}><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/></svg>";
    public const string Volume = $"<svg {A}><path d=\"M11 5 6 9H3v6h3l5 4V5Z\"/><path d=\"M15.54 8.46a5 5 0 0 1 0 7.07M19.07 4.93a10 10 0 0 1 0 14.14\"/></svg>";
    public const string SquareStop = $"<svg {A}><rect x=\"6\" y=\"6\" width=\"12\" height=\"12\" rx=\"1.5\"/></svg>";

    public static string? Resolve(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "check" => Check,
        "eye" => Eye,
        "save" => Save,
        "warning" => Warning,
        "compass" => Compass,
        "lock" => Lock,
        "target" => Target,
        "clock" or "pending" => ClockPending,
        "volume" or "speaker" => Volume,
        "stop" or "square" => SquareStop,
        _ => null
    };
}
