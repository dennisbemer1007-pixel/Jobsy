using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Applications;

/// <summary>Static scheduled-interview thread for Acc preview routes (no API).</summary>
internal static class AfterMatchMailSsrDemoData
{
    /// <summary>Matches <see cref="Jobsy.Infrastructure.Data.AfterMatchDemoIds.ScheduledApp"/> (screen 05–07 demo).</summary>
    internal static readonly Guid ScheduledApplicationId = Guid.Parse("77777771-0003-4000-8000-000000000003");

    internal static AfterMatchThreadDto ScheduledThread()
    {
        var appId = ScheduledApplicationId;
        var start = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        return new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            "9 min fietsen of 18 min met tram 2",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit, eigenaar",
            null,
            start,
            start.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-10),
            null);
    }
}
