namespace Jobsy.Core.Golf2;

/// <summary>Westland passport hint: next step comes from the career-plan dream job, not role-fit.</summary>
public static class PassportNextStepBuilder
{
    public const int MaxTitleLength = 80;

    public static string? Build(string? careerPlanDreamTitle, string? roleFitJobTitle)
    {
        _ = roleFitJobTitle;
        return NormalizeDream(careerPlanDreamTitle);
    }

    public static string? NormalizeDream(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var title = raw.Trim();
        if (title.Length > MaxTitleLength)
        {
            title = title[..MaxTitleLength].Trim();
        }

        return string.IsNullOrWhiteSpace(title) ? null : title;
    }
}
