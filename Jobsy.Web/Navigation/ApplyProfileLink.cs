namespace Jobsy.Web.Navigation;

/// <summary>
/// Where "Profiel aanvullen" sends the candidate. Work, education and references
/// live on Bewijzen. The first missing checklist item decides the page.
/// </summary>
public static class ApplyProfileLink
{
    public const string Experience = "experience";
    public const string Education = "education";
    public const string References = "references";

    public readonly record struct Gap(string Kind, bool Missing);

    public static string Href(Guid vacancyId, IReadOnlyList<Gap> checklist)
    {
        var returnPath = $"/vacancies/{vacancyId:D}#apply";
        var first = checklist.FirstOrDefault(gap => gap.Missing);
        if (first.Missing && first.Kind is Experience or Education or References)
        {
            return PassportRedirects.BuildPassportUrl(PassportTabs.Proof, returnPath)
                   + "&item=" + Uri.EscapeDataString(first.Kind);
        }

        return "/candidate/profile?returnUrl=" + Uri.EscapeDataString(returnPath);
    }
}
