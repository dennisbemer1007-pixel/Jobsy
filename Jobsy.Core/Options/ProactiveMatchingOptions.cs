namespace Jobsy.Core.Options;

public sealed class ProactiveMatchingOptions
{
    public const string SectionName = "ProactiveMatching";

    public int MinScorePercent { get; set; } = 80;
    public int MaxNotificationsPerWeek { get; set; } = 7;
    public int ShortlistTake { get; set; } = 25;
}
