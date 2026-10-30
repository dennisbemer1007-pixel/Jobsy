using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMomentCopy
{
    internal static string FormatLeadPick(CultureState culture, string contact, string company, int openSlotCount)
    {
        var count = Math.Max(0, openSlotCount);
        if (count == 1)
        {
            return string.Format(
                culture["AfterMatch.Screen04.Hero.LeadPickOne"],
                contact,
                company);
        }

        return string.Format(
            culture["AfterMatch.Screen04.Hero.LeadPick"],
            contact,
            company,
            count);
    }
}
