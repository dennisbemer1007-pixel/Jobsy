namespace Jobsy.Web.UwvFeatures;

/// <summary>
/// Local-only gate for UWV werkmap mock blocks on real candidate pages.
/// Default off; enable via <c>Features:UwvFeaturesMockup</c> or <c>JOBSY_UWV_FEATURES_MOCKUP=1</c>.
/// </summary>
public interface IUwvFeaturesMockupGate
{
    bool IsEnabled { get; }
}

public sealed class UwvFeaturesMockupGate(IConfiguration configuration) : IUwvFeaturesMockupGate
{
    public bool IsEnabled { get; } =
        configuration.GetValue("Features:UwvFeaturesMockup", false)
        || string.Equals(Environment.GetEnvironmentVariable("JOBSY_UWV_FEATURES_MOCKUP"), "1", StringComparison.Ordinal);
}
