namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigMockupRoute
{
    public sealed record Resolved(
        OnboardingRustigMockupScreen Meta,
        OnboardingRustigMockupView View,
        bool ShowOverviewResultRail = false);

    public static Resolved? Resolve(string screen)
    {
        var s = screen.Trim().ToLowerInvariant();
        if (string.Equals(s, "index", StringComparison.Ordinal))
            return null;

        if (s is "step-07" or "step-07-a")
            return new Resolved(Require("step-07"), OnboardingRustigMockupView.TestQuestion);
        if (s is "step-07-b")
            return new Resolved(Require("step-07"), OnboardingRustigMockupView.TestsOverview, ShowOverviewResultRail: true);

        if (s.EndsWith("-intro", StringComparison.Ordinal))
        {
            var slug = s[..^6];
            return new Resolved(RequireTestSlug(slug), OnboardingRustigMockupView.TestIntro);
        }

        if (s.EndsWith("-result", StringComparison.Ordinal))
        {
            var slug = s[..^7];
            return new Resolved(RequireTestSlug(slug), OnboardingRustigMockupView.TestResult);
        }

        if (s.EndsWith("-vraag", StringComparison.Ordinal))
        {
            var slug = s[..^6];
            return new Resolved(RequireTestSlug(slug), OnboardingRustigMockupView.TestQuestion);
        }

        // Legacy aliases
        if (s is "test-leuk" or "test-leuk-a" or "test-leuk-b")
            return new Resolved(Require("step-09"), OnboardingRustigMockupView.TestQuestion);

        var meta = OnboardingRustigMockupRegistry.Find(s);
        if (meta is null)
            return null;

        var view = meta.Kind switch
        {
            OnboardingRustigMockupKind.TestOverview => OnboardingRustigMockupView.TestsOverview,
            OnboardingRustigMockupKind.TestQuestion => OnboardingRustigMockupView.TestQuestion,
            _ => OnboardingRustigMockupView.Default,
        };
        return new Resolved(meta, view);
    }

    private static OnboardingRustigMockupScreen Require(string slug) =>
        OnboardingRustigMockupRegistry.Find(slug)
        ?? throw new InvalidOperationException($"Missing mockup slug {slug}");

    private static OnboardingRustigMockupScreen RequireTestSlug(string slug)
    {
        var meta = OnboardingRustigMockupRegistry.Find(slug);
        if (meta?.Kind != OnboardingRustigMockupKind.TestQuestion)
            throw new InvalidOperationException($"Not a test slug: {slug}");
        return meta;
    }
}
