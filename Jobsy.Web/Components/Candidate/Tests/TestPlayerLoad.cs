namespace Jobsy.Web.Components.Candidate.Tests;

/// <summary>
/// Bound for the deep-test player and the consent gate. The shared API client
/// waits up to 45 minutes; without this cap a stalled read stays on the skeleton.
/// </summary>
public static class TestPlayerLoad
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(12);
}
