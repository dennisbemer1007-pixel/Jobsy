using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Mini / deeper / full question id ordering for Quick-Scan v2 item banks.
/// Mini = first id per scale block; deeper = second id per block (5 + 5 = 10, then full catalog).
/// </summary>
public static class OnboardingWizardCatalogV2
{
    public static readonly int[] CompetencyQuestionIds = [1, 9, 17, 25, 33];

    public static readonly int[] CompetencyDeeperQuestionIds = [2, 10, 18, 26, 34];

    /// <summary>Five RIASEC blocks in mini (Artistic measured in deeper / full).</summary>
    public static readonly int[] CareerQuestionIds = [1, 9, 25, 33, 41];

    public static readonly int[] CareerDeeperQuestionIds = [2, 10, 18, 26, 34];

    public static readonly int[] CultureQuestionIds = [1, 3, 5, 7, 11];

    public static readonly int[] CultureDeeperQuestionIds = [2, 4, 6, 8, 12];

    public static readonly int[] ValuesQuestionIds = [1, 9, 17, 25, 33];

    public static readonly int[] ValuesDeeperQuestionIds = [2, 10, 18, 26, 34];

    public static int[] MiniIds(OnboardingWizardCatalog.OnboardingTestKind kind) => kind switch
    {
        OnboardingWizardCatalog.OnboardingTestKind.Competency => CompetencyQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Career => CareerQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Culture => CultureQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Values => ValuesQuestionIds,
        _ => []
    };

    public static int[] DeeperIds(OnboardingWizardCatalog.OnboardingTestKind kind) => kind switch
    {
        OnboardingWizardCatalog.OnboardingTestKind.Competency => CompetencyDeeperQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Career => CareerDeeperQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Culture => CultureDeeperQuestionIds,
        OnboardingWizardCatalog.OnboardingTestKind.Values => ValuesDeeperQuestionIds,
        _ => []
    };

    public static IReadOnlyList<int> AllCatalogIds(OnboardingWizardCatalog.OnboardingTestKind kind) => kind switch
    {
        OnboardingWizardCatalog.OnboardingTestKind.Competency =>
            CompetencyTestCatalogV2.Questions.Select(q => q.Id).ToArray(),
        OnboardingWizardCatalog.OnboardingTestKind.Career =>
            CareerTestCatalogV2.Questions.Select(q => q.Id).ToArray(),
        OnboardingWizardCatalog.OnboardingTestKind.Culture =>
            CulturePersonalityCatalogV2.Questions.Select(q => q.Id).ToArray(),
        OnboardingWizardCatalog.OnboardingTestKind.Values =>
            SchwartzValuesCatalogV2.Questions.Select(q => q.Id).ToArray(),
        _ => []
    };

    public static int[] MiniIds(AssessmentKind kind)
        => MiniIds(TestDepthRules.ToOnboardingKind(kind));

    public static int[] DeeperIds(AssessmentKind kind)
        => DeeperIds(TestDepthRules.ToOnboardingKind(kind));

    public static IReadOnlyList<int> AllCatalogIds(AssessmentKind kind)
        => AllCatalogIds(TestDepthRules.ToOnboardingKind(kind));
}
