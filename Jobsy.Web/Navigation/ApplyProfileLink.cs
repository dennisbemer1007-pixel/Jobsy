using Jobsy.Core.Rules;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Deep links for vacancy apply checklist items (basics + vacancy-specific proof fields).
/// </summary>
public static class ApplyProfileLink
{
    public const string Experience = "experience";
    public const string Education = "education";
    public const string References = "references";

    public readonly record struct Gap(string Kind, bool Missing, string? MessageKey = null);

    public static string Href(Guid vacancyId, IReadOnlyList<Gap> checklist)
        => HrefForKind(vacancyId, checklist.FirstOrDefault(g => g.Missing).Kind);

    public static string HrefForKind(Guid vacancyId, string? kind)
    {
        var returnPath = $"/vacancies/{vacancyId:D}#apply";
        if (string.IsNullOrWhiteSpace(kind))
        {
            return PassportRedirects.BuildPassportUrl(PassportTabs.Data, returnPath) + "&section=personal";
        }

        return kind switch
        {
            Experience or Education or References =>
                PassportRedirects.BuildPassportUrl(PassportTabs.Proof, returnPath)
                + "&item=" + Uri.EscapeDataString(kind),
            "work-status" =>
                PassportRedirects.BuildPassportUrl(PassportTabs.Data, returnPath) + "&section=work-status",
            "license" or "personal" or "name" or "birthdate" or "location" =>
                PassportRedirects.BuildPassportUrl(PassportTabs.Data, returnPath) + "&section=personal",
            "hours" or "availability" or "availability-start" or "open-for-work" =>
                PassportRedirects.BuildPassportUrl(PassportTabs.Data, returnPath) + "&section=availability",
            _ => "/candidate/start?returnUrl=" + Uri.EscapeDataString(returnPath)
        };
    }

    public static string KindFor(ApplyBasicsField field)
        => field switch
        {
            ApplyBasicsField.Name or ApplyBasicsField.BirthDate => "personal",
            ApplyBasicsField.Location => "location",
            ApplyBasicsField.WorkStatus => "work-status",
            ApplyBasicsField.Hours or ApplyBasicsField.Availability
                or ApplyBasicsField.AvailabilityStart or ApplyBasicsField.OpenForWork => "availability",
            _ => "personal"
        };
}
