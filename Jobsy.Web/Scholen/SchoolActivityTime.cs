using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

/// <summary>
/// One "Laatst actief" line for teacher and school tables.
/// Same calendar day is "vandaag HH:mm", the day before is "gisteren HH:mm".
/// </summary>
public static class SchoolActivityTime
{
    public static string Format(DateTime utc)
    {
        var local = LobsyTime.ToAmsterdam(utc);
        var today = LobsyTime.ToAmsterdam(DateTime.UtcNow).Date;
        var clock = local.ToString("HH:mm");
        if (local.Date == today)
        {
            return "vandaag " + clock;
        }

        if (local.Date == today.AddDays(-1))
        {
            return "gisteren " + clock;
        }

        return local.ToString("dd-MM HH:mm");
    }
}
