namespace Jobsy.Web.Onboarding;

/// <summary>Development E2E hooks (Playwright). Null in production traffic.</summary>
public static class OnboardingSsrE2eOverrides
{
    public static IReadOnlyList<string>? DreamTitles { get; set; }
}
