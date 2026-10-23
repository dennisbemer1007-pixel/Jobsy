using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrRedirect
{
    public static async Task<string?> TryGetRedirectUrlAsync(
        HttpRequest request,
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        DiscoveryOverviewCache overviewCache,
        OnboardingState? state,
        OnboardingProfileDraft draft,
        string? stapRaw,
        string? faseRaw,
        string? diepteRaw,
        CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return null;
        }

        var panel = await overviewCache.GetPanelAsync(cancellationToken);
        var journeyComplete = DiscoverySsrJourneyState.IsJourneyComplete(state, draft);
        var openFase = DiscoverySsrJourneyState.ResolveOpenPhaseId(draft, state, panel);
        var stap = NormalizeStap(stapRaw);

        if (journeyComplete)
        {
            if (string.IsNullOrEmpty(stap)
                || stap is "start"
                || (int.TryParse(stap, out var n) && n is >= 1 and <= 10))
            {
                return CanonicalOverview(openFase);
            }

            if (string.Equals(stap, "overview", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(faseRaw))
            {
                return CanonicalOverview(openFase);
            }
        }
        else if (string.IsNullOrWhiteSpace(stapRaw))
        {
            var screen = DiscoverySsrJourneyState.ResolveScreen(null, state);
            if (string.Equals(screen, "overview", StringComparison.OrdinalIgnoreCase))
            {
                return CanonicalOverview(openFase);
            }

            if (int.TryParse(screen, out var step) && step is >= 1 and <= 10)
            {
                return DiscoverySsrUrls.Step(step);
            }

            if (string.Equals(screen, "start", StringComparison.OrdinalIgnoreCase))
            {
                return DiscoverySsrUrls.WithStap("start");
            }
        }

        if (int.TryParse(stap, out var testStep) && testStep is >= 7 and <= 10)
        {
            var targetLevel = OnboardingWizardCatalog.MiniLevelCount;
            if (JourneyTestFlow.TryParseDepth(diepteRaw, out var depth))
            {
                targetLevel = depth;
            }

            var band = await DiscoverySsrMiniLoader.LoadBandAsync(
                testStep,
                targetLevel,
                api,
                featureFlags,
                culture,
                cancellationToken);
            if (band.BandComplete || band.Questions.Count == 0)
            {
                if (journeyComplete)
                {
                    return DiscoverySsrUrls.Overview(openFase);
                }

                return DiscoverySsrUrls.Shed(testStep);
            }
        }

        return null;
    }

    public static bool IsSameLocation(HttpRequest request, string targetRelativeUrl)
    {
        var current = (request.Path.Value ?? "") + (request.QueryString.Value ?? "");
        var normalizedCurrent = NormalizeUrl(current);
        var normalizedTarget = NormalizeUrl(targetRelativeUrl);
        return string.Equals(normalizedCurrent, normalizedTarget, StringComparison.OrdinalIgnoreCase);
    }

    private static string CanonicalOverview(string fase)
        => DiscoverySsrUrls.Overview(fase);

    private static string? NormalizeStap(string? stapRaw)
        => string.IsNullOrWhiteSpace(stapRaw) ? null : stapRaw.Trim().ToLowerInvariant();

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "/candidate/ontdekkingsreis";
        }

        var qIdx = url.IndexOf('?', StringComparison.Ordinal);
        var path = qIdx >= 0 ? url[..qIdx] : url;
        if (qIdx < 0)
        {
            return path;
        }

        var query = QueryHelpers.ParseQuery(url[qIdx..]);
        var pairs = query
            .OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .SelectMany(kv => kv.Value.Select(v => $"{kv.Key}={v}"))
            .ToList();
        return pairs.Count == 0 ? path : $"{path}?{string.Join("&", pairs)}";
    }
}
