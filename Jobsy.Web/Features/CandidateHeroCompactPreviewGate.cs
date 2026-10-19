using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Features;

public enum CandidateHeroCompactPreviewVariant
{
    None,
    A,
    B,
    C
}

/// <summary>
/// Query-string preview for owner review: ?heroPreview=a (~170px), ?heroPreview=b (~130px),
/// ?heroPreview=c (B height + wide seamless art on illustration cream).
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
    public CandidateHeroCompactPreviewVariant Variant => Parse(navigation.Uri);

    public string? CandHeroCssClass => Variant switch
    {
        CandidateHeroCompactPreviewVariant.A => "cand-hero--compact-preview-a",
        CandidateHeroCompactPreviewVariant.B => "cand-hero--compact-preview-b",
        CandidateHeroCompactPreviewVariant.C => "cand-hero--compact-preview-c",
        _ => null
    };

    public string? OnboardingStepCssClass => Variant switch
    {
        CandidateHeroCompactPreviewVariant.A => "ob-illustrated-step--compact-preview-a",
        CandidateHeroCompactPreviewVariant.B => "ob-illustrated-step--compact-preview-b",
        CandidateHeroCompactPreviewVariant.C => "ob-illustrated-step--compact-preview-c",
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
            "c" => CandidateHeroCompactPreviewVariant.C,
            _ => CandidateHeroCompactPreviewVariant.None
        };
    }
}
