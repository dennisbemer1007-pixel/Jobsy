namespace Jobsy.Core.Features;

/// <summary>
/// Gates a page, controller, or action on a <see cref="PlatformFeature"/> flag.
/// When the requirement is not met, API returns 404 feature_disabled; Web redirects to fallback.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresFeatureAttribute : Attribute
{
    public RequiresFeatureAttribute(PlatformFeature feature, bool whenEnabled = true)
    {
        Feature = feature;
        WhenEnabled = whenEnabled;
    }

    public PlatformFeature Feature { get; }

    /// <summary>When true (default), the feature must be ON. When false, the feature must be OFF.</summary>
    public bool WhenEnabled { get; }

    /// <summary>Optional explicit fallback path for Web; otherwise <c>FeatureRoutes.HomeFor</c>.</summary>
    public string? FallbackPath { get; set; }
}
