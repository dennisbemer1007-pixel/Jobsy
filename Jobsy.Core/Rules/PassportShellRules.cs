namespace Jobsy.Core.Rules;

/// <summary>
/// Derived "Mijn schalen" stamps — no storage, no dates. Pure rules for the passport.
/// </summary>
public static class PassportShellRules
{
    public const string FirstTest = "first-test";
    public const string FirstReport = "first-report";
    public const string CvAdded = "cv-added";
    public const string ThreeTests = "three-tests";
    public const string DnaComplete = "dna-complete";
    public const string FirstApplication = "first-application";

    public sealed record ShellDefinition(
        string Code,
        string TitleKey,
        bool Earned,
        bool Visible);

    public sealed record ShellProgress(
        IReadOnlyList<ShellDefinition> Shells,
        int EarnedCount,
        int TotalVisible,
        int QuestionsUntilNextShell);

    public static ShellProgress Evaluate(
        int completedTests,
        int extendedReportsCompleted,
        bool hasOwnCvOrTrackedLobsyCv,
        int applicationCount,
        bool employersEnabled,
        int remainingQuestionsNearestUnfinished)
    {
        var shells = new List<ShellDefinition>
        {
            new(FirstTest, "Passport.Shells.Item.FirstTest", completedTests >= 1, true),
            new(FirstReport, "Passport.Shells.Item.FirstReport", extendedReportsCompleted >= 1, true),
            new(CvAdded, "Passport.Shells.Item.Cv", hasOwnCvOrTrackedLobsyCv, true),
            new(ThreeTests, "Passport.Shells.Item.ThreeTests", completedTests >= 3, true),
            new(DnaComplete, "Passport.Shells.Item.DnaComplete", completedTests >= 4, true),
            new(
                FirstApplication,
                "Passport.Shells.Item.FirstJob",
                applicationCount >= 1,
                employersEnabled)
        };

        var visible = shells.Where(s => s.Visible).ToList();
        var earned = visible.Count(s => s.Earned);
        var remaining = Math.Max(0, remainingQuestionsNearestUnfinished);
        return new ShellProgress(visible, earned, visible.Count, remaining);
    }
}
