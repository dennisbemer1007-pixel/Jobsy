using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Features;

/// <summary>Quick-Scan item examples when <see cref="PlatformFeature.TestItemExamples"/> is on.</summary>
public static class TestItemExamples
{
    public static async ValueTask<string?> ForQuickScanAsync(
        IFeatureFlags flags,
        AssessmentKind kind,
        int id,
        string domain,
        bool reverse,
        string lang,
        CancellationToken cancellationToken = default)
    {
        if (!await flags.IsEnabledAsync(PlatformFeature.TestItemExamples, cancellationToken))
        {
            return null;
        }

        return AssessmentItemExamples.ExampleFor(kind, id, domain, reverse, lang);
    }
}
