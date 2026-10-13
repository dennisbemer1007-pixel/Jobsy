namespace Jobsy.Web.Services.Careers;

/// <summary>Deterministic hrefs for career step actions (D17); API returns kinds only.</summary>
public static class CareerStepActionLinks
{
    public const string CoursesAnchor = "#career-courses";

    public static string VacanciesSearchHref(string stepTitle)
    {
        var q = (stepTitle ?? "").Trim();
        if (q.Length == 0)
        {
            return "/";
        }

        return "/?q=" + Uri.EscapeDataString(q);
    }

    /// <summary>Primary legacy <c>ActionHref</c> for the old /carriere UI.</summary>
    public static string PrimaryHref(IReadOnlyList<string>? actionKinds, string stepTitle, string? legacyActionHref)
    {
        if (actionKinds is { Count: > 0 })
        {
            foreach (var kind in actionKinds)
            {
                if (string.Equals(kind, "Vacancies", StringComparison.OrdinalIgnoreCase))
                {
                    return VacanciesSearchHref(stepTitle);
                }
            }

            foreach (var kind in actionKinds)
            {
                if (string.Equals(kind, "Courses", StringComparison.OrdinalIgnoreCase))
                {
                    return CoursesAnchor;
                }
            }

            return "";
        }

        return legacyActionHref ?? "";
    }
}
