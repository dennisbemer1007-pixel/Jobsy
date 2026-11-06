namespace Jobsy.Web.Components.Candidate.Discovery;

/// <summary>Progress chrome for the discovery journey rail.</summary>
public static class JourneyChrome
{
    /// <summary>
    /// A finished journey shows 10 of 10 on every screen, including a revisited step.
    /// </summary>
    public static bool ShowFinished(bool journeyFinished, bool isLightScreen)
        => journeyFinished || isLightScreen;

    public static int LayersOff(bool showFinished, string? screen, int railStep, int shedStep)
    {
        if (showFinished)
        {
            return 10;
        }

        if (string.Equals(screen, "shed", StringComparison.Ordinal))
        {
            return shedStep;
        }

        return Math.Max(railStep - 1, 0);
    }
}
