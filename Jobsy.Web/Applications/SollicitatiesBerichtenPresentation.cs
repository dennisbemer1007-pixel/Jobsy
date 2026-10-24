using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class SollicitatiesBerichtenPresentation
{
    internal static string ThreadListPreview(CultureState culture, SollicitationRow row)
    {
        var summary = row.AfterMatch!;
        if (!string.IsNullOrWhiteSpace(summary.LastMessagePreview))
        {
            var prefix = summary.LastMessageFromCandidate
                ? culture["Apps.Berichten.PreviewYou"]
                : string.Empty;
            return prefix + summary.LastMessagePreview;
        }

        return row.CardHint ?? row.StatusLabel;
    }

    internal static string ThreadSubtitle(CultureState culture, SollicitationRow row, AfterMatchThreadDto? thread)
    {
        var contact = thread?.WithWhom;
        if (string.IsNullOrWhiteSpace(contact))
        {
            contact = row.CompanyName;
        }

        var role = row.Title;
        if (thread?.ScheduledStartUtc is DateTime start)
        {
            var when = FormatShortDate(culture, start);
            return string.Format(
                culture["Apps.Berichten.SubtitleInterview"],
                contact,
                when);
        }

        return $"{contact} · {role}";
    }

    internal static string? BothYesSystemLine(CultureState culture, AfterMatchThreadDto thread)
    {
        if (thread.AcceptedAtUtc is not DateTime at)
        {
            return null;
        }

        return string.Format(
            culture["Apps.Berichten.SysBothYes"],
            FormatShortDate(culture, at));
    }

    internal static string FormatMessageMeta(CultureState culture, AfterMatchMessageDto message)
    {
        var when = FormatMessageWhen(culture, message.CreatedAtUtc);
        var line = $"{message.SenderLabel} · {when}";
        if (message.IsMine && message.ReadAtUtc is DateTime readAt)
        {
            line += string.Format(
                culture["Apps.Berichten.ReadSuffix"],
                FormatShortDate(culture, readAt));
        }

        return line;
    }

    internal static string FormatMessageWhen(CultureState culture, DateTime utc)
    {
        var local = utc.ToLocalTime();
        return local.ToString("d MMM, HH:mm", ApplicationsSsrData.FormatCulture(culture));
    }

    internal static string FormatShortDate(CultureState culture, DateTime utc)
    {
        var local = utc.ToLocalTime();
        return local.ToString("d MMM", ApplicationsSsrData.FormatCulture(culture));
    }

    internal static string? SlotProposalSystemLine(CultureState culture, AfterMatchThreadDto thread)
    {
        if (thread.OpenSlots.Count == 0)
        {
            return null;
        }

        return string.Format(
            culture["Apps.Berichten.SysSlots"],
            thread.OpenSlots.Count,
            FormatShortDate(culture, thread.OpenSlots[0].StartUtc));
    }
}
