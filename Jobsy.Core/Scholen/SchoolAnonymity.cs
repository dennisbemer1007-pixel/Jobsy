namespace Jobsy.Core.Scholen;

public static class SchoolAnonymity
{
    /// <summary>Group results and aggregates require at least this many completed pupils.</summary>
    public const int MinGroupSize = 5;

    /// <summary>Dream jobs chosen by fewer pupils than this show as "Overig".</summary>
    public const int MinDreamJobCount = 2;
}
