using Jobsy.Web.Features;

namespace Jobsy.Tests;

/// <summary>Default preview gate for Bunit (no ?heroPreview).</summary>
public sealed class CandidateHeroCompactPreviewGateStub : ICandidateHeroCompactPreviewGate
{
    public static CandidateHeroCompactPreviewGateStub None { get; } = new();

    public CandidateHeroCompactPreviewVariant Variant => CandidateHeroCompactPreviewVariant.None;

    public string? CandHeroCssClass => null;

    public string? OnboardingStepCssClass => null;
}
