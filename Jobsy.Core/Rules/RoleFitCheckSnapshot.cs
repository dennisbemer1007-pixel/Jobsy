namespace Jobsy.Core.Rules;

public sealed record RoleFitCheckSnapshot(
    string JobTitle,
    int MatchPercent,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> ActionSteps,
    IReadOnlyList<string> SearchKeys,
    bool FromDeepAnalysis,
    bool FromOpenAi = false)
{
    public string MapQuery
    {
        get
        {
            if (SearchKeys.Count > 0)
            {
                return string.Join(' ', SearchKeys.Take(3));
            }

            return JobTitle;
        }
    }

    public bool ShowDeepUpsell => !FromDeepAnalysis;
}
