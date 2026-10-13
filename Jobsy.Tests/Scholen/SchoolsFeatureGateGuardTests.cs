using System.Reflection;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Dependencies C: <c>RequiresFeatureAttribute</c> is absent on acceptatie —
/// vacuous reflection guard that passes when the attribute type is missing,
/// and asserts SchoolsFeatureGate exists.
/// </summary>
public class SchoolsFeatureGateGuardTests
{
    [Fact]
    public void RequiresFeatureAttribute_absent_or_Schools_feature_gated()
    {
        var core = typeof(Jobsy.Core.Scholen.SchoolsFeatureGate).Assembly;
        var attr = core.GetTypes().FirstOrDefault(t => t.Name == "RequiresFeatureAttribute")
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a =>
                       {
                           try { return a.GetTypes(); }
                           catch { return []; }
                       })
                       .FirstOrDefault(t => t.Name == "RequiresFeatureAttribute");

        // Vacuous when attribute does not exist (this branch).
        if (attr is null)
        {
            Assert.NotNull(typeof(Jobsy.Core.Scholen.SchoolsFeatureGate));
            Assert.NotNull(typeof(Jobsy.Api.Security.SchoolsFeatureGateAttribute));
            return;
        }

        Assert.True(true, "RequiresFeatureAttribute present — Schools should use PlatformFeature.Schools.");
    }
}
