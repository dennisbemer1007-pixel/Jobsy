namespace Jobsy.Web.Features;

/// <summary>
/// Web-only toggle for the illustrated "Wie ben jij" test journey intro (step 7).
/// Env/config key: <c>TestJourneyIllustrationEnabled</c> (default false).
/// </summary>
public interface ITestJourneyIllustrationGate
{
    bool Enabled { get; }
}

public sealed class TestJourneyIllustrationGate(IConfiguration configuration) : ITestJourneyIllustrationGate
{
    public bool Enabled => configuration.GetValue("TestJourneyIllustrationEnabled", false);
}
