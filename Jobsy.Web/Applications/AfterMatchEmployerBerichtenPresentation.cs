using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerBerichtenPresentation
{
    internal static bool ThreadEligible(AfterMatchThreadDto thread)
        => thread.AcceptedAtUtc is not null
           || thread.Messages.Count > 0
           || thread.InterviewStatus is InterviewStatus.Scheduled
               or InterviewStatus.Done
               or InterviewStatus.RescheduleRequested;

    internal static string DisplayName(CultureState culture, EmployerApplicationItem item)
        => ApplicationPipelineRules.DisplayName(
            item,
            culture["WgApp.Anonymous"]);

    internal static string Initials(EmployerApplicationItem item)
    {
        var fromName = ApplicationPipelineRules.Initials(item);
        if (!string.IsNullOrWhiteSpace(fromName))
        {
            return fromName;
        }

        return "#" + ApplicationPipelineRules.ShortId(item.Id)[0];
    }

    internal static string AvatarClass(int index) => (index % 3) switch
    {
        1 => "g",
        2 => "",
        _ => "w",
    };

    internal static int UnreadFromCandidate(AfterMatchThreadDto thread)
        => thread.Messages.Count(m => !m.IsMine && m.ReadAtUtc is null);

    internal static string ThreadSubtitle(
        CultureState culture,
        AfterMatchThreadDto thread)
    {
        var role = thread.VacancyTitle;
        if (thread.ScheduledStartUtc is DateTime start)
        {
            var when = AfterMatchMailSsrTime.ToAmsterdam(start).ToString(
                "dddd d MMMM, HH:mm",
                ApplicationsSsrData.FormatCulture(culture));
            return $"{role} · gesprek {when}";
        }

        if (thread.Outcome == AfterMatchOutcomeKind.Offer)
        {
            return $"{role} · {culture["AfterMatch.Screen12.Offer"]}";
        }

        return role;
    }

    internal static string FormatMessageMeta(
        CultureState culture,
        AfterMatchThreadDto thread,
        AfterMatchMessageDto message)
    {
        var when = SollicitatiesBerichtenPresentation.FormatMessageWhen(culture, message.CreatedAtUtc);
        var line = $"{message.SenderLabel} · {when}";
        if (message.IsMine && message.ReadAtUtc is DateTime)
        {
            line += string.Format(
                culture["AfterMatch.Messages.ReadBy"],
                thread.CandidateName);
        }

        return line;
    }

    internal static string? BothYesSystemLine(CultureState culture, AfterMatchThreadDto thread)
        => SollicitatiesBerichtenPresentation.BothYesSystemLine(culture, thread);
}
