namespace Jobsy.Web.OnboardingRustigMockups;

/// <summary>Which static body layout to render for a mockup route (hero uses base slug panoramas).</summary>
public enum OnboardingRustigMockupView
{
    Default,
    /// <summary>Four-test overview (step-07).</summary>
    TestsOverview,
    /// <summary>Short intro before likert questions (per test).</summary>
    TestIntro,
    /// <summary>Likert question screen.</summary>
    TestQuestion,
    /// <summary>Result / transition after a test.</summary>
    TestResult,
}
