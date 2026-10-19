using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Features;

public enum CandidateHeroCompactPreviewVariant
{
    None,
    A,
    B
}

/// <summary>
/// Query-string preview for owner review: ?heroPreview=a (~170px band) or ?heroPreview=b (~130px).
/// Only wired on selected pages — not a global rollout.
/// </summary>
public interface ICandidateHeroCompactPreviewGate
{
    CandidateHeroCompactPreviewVariant Variant { get; }

    string? CandHeroCssClass { get; }

    string? OnboardingStepCssClass { get; }
}

public sealed class CandidateHeroCompactPreviewGate(NavigationManager navigation) : ICandidateHeroCompactPreviewGate
{
    public CandidateHeroCompactPreviewVariant Variant { get; } = Parse(navigation.Uri);

    public string? CandHeroCssClass => Variant switch
    {
        CandidateHeroCompactPreviewVariant.A => "cand-hero--compact-preview-a",
        CandidateHeroCompactPreviewVariant.B => "cand-hero--compact-preview-b",
        _ => null
    };

    public string? OnboardingStepCssClass => Variant switch
    {
        CandidateHeroCompactPreviewVariant.A => "ob-illustrated-step--compact-preview-a",
        CandidateHeroCompactPreviewVariant.B => "ob-illustrated-step--compact-preview-b",
        _ => null
    };

    private static CandidateHeroCompactPreviewVariant Parse(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return CandidateHeroCompactPreviewVariant.None;
        }

        var query = QueryHelpers.ParseQuery(absolute.Query);
        if (!query.TryGetValue("heroPreview", out var raw))
        {
            return CandidateHeroCompactPreviewVariant.None;
        }

        return raw.ToString().Trim().ToLowerInvariant() switch
        {
            "a" => CandidateHeroCompactPreviewVariant.A,
            "b" => CandidateHeroCompactPreviewVariant.B,
            _ => CandidateHeroCompactPreviewVariant.None
        };
    }
}
