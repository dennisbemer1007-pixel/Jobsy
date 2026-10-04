using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>
/// Status label for a code row. Login sets last-seen before the first answer,
/// so "Nog niet gestart" would hide that the pupil already signed in.
/// </summary>
public static class PupilCodeStatusText
{
    public static string Label(
        CultureState culture,
        PupilCodeStatus status,
        int progressCurrent,
        int progressTotal,
        DateTime? lastSeenAtUtc)
        => status switch
        {
            PupilCodeStatus.Completed => culture["School.Status.Completed"],
            PupilCodeStatus.InProgress => string.Format(
                culture["School.Status.InProgress"],
                progressCurrent,
                progressTotal),
            PupilCodeStatus.NotStarted when lastSeenAtUtc is not null => culture["School.Status.LoggedIn"],
            _ => culture["School.Status.NotStarted"]
        };
}
