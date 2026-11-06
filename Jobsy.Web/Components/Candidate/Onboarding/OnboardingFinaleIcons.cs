namespace Jobsy.Web.Components.Candidate.Onboarding;

internal static class OnboardingFinaleIcons
{
    private static string Svg(string inner)
        => $"""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">{inner}</svg>""";

    public static string Match => Svg("""<path d="M12 21s-7-4.5-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 11c0 5.5-7 10-7 10z"/>""");
    public static string Map => Svg("""<path d="M9 4 3 6v14l6-2 6 2 6-2V4l-6 2-6-2z"/><path d="M9 4v14M15 6v14"/>""");
    public static string Compass => Svg("""<circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2 5-5 2 2-5 5-2z"/>""");
    public static string Route => Svg("""<circle cx="6" cy="18" r="2"/><circle cx="18" cy="6" r="2"/><path d="M8 18h6a3 3 0 0 0 0-6h-4a3 3 0 0 1 0-6h6"/>""");
    public static string Passport => Svg("""<rect x="5" y="3" width="14" height="18" rx="2"/><circle cx="12" cy="10" r="3"/><path d="M8.5 17h7"/>""");
    public static string Deeper => Svg("""<path d="M4 18 10 12l4 4 6-8"/><path d="M15 8h5v5"/>""");
    public static string References => Svg("""<path d="M12 3 5 6v5c0 4.5 3 8 7 10 4-2 7-5.5 7-10V6l-7-3z"/><path d="m9 12 2 2 4-4"/>""");
    public static string Bell => Svg("""<path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4l2-2z"/><path d="M10 21h4"/>""");
    public static string Employers => Svg("""<rect x="3" y="7" width="18" height="13" rx="2"/><path d="M9 7V5h6v2"/><circle cx="12" cy="13" r="2"/>""");
    public static string Star => Svg("""<path d="m12 4 2.4 5 5.6.6-4.2 3.8 1.2 5.6L12 16.2 7 19l1.2-5.6L4 9.6 9.6 9 12 4z"/>""");
    public static string Leaf => Svg("""<path d="M5 19c0-8 6-13 14-14-1 8-6 14-14 14z"/><path d="M5 19 13 11"/>""");
    public static string Heart => Svg("""<path d="M12 20s-7-4.4-7-9.5A4 4 0 0 1 12 8a4 4 0 0 1 7 2.5C19 15.6 12 20 12 20z"/>""");
}
