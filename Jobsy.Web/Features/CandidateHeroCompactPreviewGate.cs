using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Features;

public enum CandidateHeroCompactPreviewVariant
{
    None,
    A,
    B,
    C
}

/// <summary>
/// Compact cream (#FAEBDB) candidate hero band (~130–150px). On by default; admin/config can disable.
/// Legacy tall hero: ?heroPreview=legacy or Jobsy:CandidateCompactHeroEnabled=false.
/// </summary>
public interface ICandidateHeroCompactPreviewGate
{
    bool IsEnabled { get; }

    CandidateHeroCompactPreviewVariant Variant { get; }

    string? CandHeroCssClass { get; }

    string? OnboardingStepCssClass { get; }
}

public sealed class CandidateHeroCompactPreviewGate(
    NavigationManager navigation,
    IConfiguration configuration) : ICandidateHeroCompactPreviewGate
{
    public bool IsEnabled => configuration.GetValue("Jobsy:CandidateCompactHeroEnabled", true);

    public CandidateHeroCompactPreviewVariant Variant => ResolveVariant(navigation.Uri, IsEnabled);

    public string? CandHeroCssClass => Variant == CandidateHeroCompactPreviewVariant.C
        ? "cand-hero--compact-preview-c"
        : Variant switch
        {
            CandidateHeroCompactPreviewVariant.A => "cand-hero--compact-preview-a",
            CandidateHeroCompactPreviewVariant.B => "cand-hero--compact-preview-b",
            _ => null
        };

    public string? OnboardingStepCssClass => Variant == CandidateHeroCompactPreviewVariant.C
        ? "ob-illustrated-step--compact-preview-c"
        : Variant switch
        {
            CandidateHeroCompactPreviewVariant.A => "ob-illustrated-step--compact-preview-a",
            CandidateHeroCompactPreviewVariant.B => "ob-illustrated-step--compact-preview-b",
            _ => null
        };

    internal static CandidateHeroCompactPreviewVariant ResolveVariant(string uri, bool enabled)
    {
        if (!enabled)
        {
            return CandidateHeroCompactPreviewVariant.None;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return CandidateHeroCompactPreviewVariant.C;
        }

        var query = QueryHelpers.ParseQuery(absolute.Query);
        if (!query.TryGetValue("heroPreview", out var raw))
        {
            return CandidateHeroCompactPreviewVariant.C;
        }

        return raw.ToString().Trim().ToLowerInvariant() switch
        {
            "legacy" or "none" or "off" => CandidateHeroCompactPreviewVariant.None,
            "a" => CandidateHeroCompactPreviewVariant.A,
            "b" => CandidateHeroCompactPreviewVariant.B,
            "c" => CandidateHeroCompactPreviewVariant.C,
            _ => CandidateHeroCompactPreviewVariant.C
        };
    }
}
