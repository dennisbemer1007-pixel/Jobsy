using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Components.AfterMatch;

internal static class NdMDemoData
{
    public static AfterMatchMessageDto DemoMessage() =>
        new(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            false,
            "Linda de Wit",
            "Hoi Sanne, leuk dat je wilt komen kennismaken! Ik stuur je vandaag nog een paar momenten.",
            new DateTime(2026, 10, 13, 13, 40, 0, DateTimeKind.Utc),
            null,
            ApplicationMessageBlockReason.None,
            false);

    public static IReadOnlyList<AfterMatchSlotDto> DemoSlots()
    {
        var baseStart = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        return
        [
            new AfterMatchSlotDto(Guid.NewGuid(), baseStart, baseStart.AddMinutes(30), InterviewSlotProposer.Employer, null, false),
            new AfterMatchSlotDto(Guid.NewGuid(), baseStart.AddHours(2), baseStart.AddHours(2).AddMinutes(30), InterviewSlotProposer.Employer, null, false),
            new AfterMatchSlotDto(Guid.NewGuid(), baseStart.AddDays(1), baseStart.AddDays(1).AddMinutes(30), InterviewSlotProposer.Employer, null, false)
        ];
    }

    public static AfterMatchThreadDto DemoThread(InterviewStatus status = InterviewStatus.Proposing) =>
        new(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Guid.Parse("22222222-3333-4444-5555-666666666666"),
            status,
            AfterMatchContactShareState.From(status, null),
            null,
            "Medewerker bakkerij",
            "Bakkerij De Korenaar",
            "9 min fietsen",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            status == InterviewStatus.Scheduled ? DemoSlots()[0].StartUtc : null,
            status == InterviewStatus.Scheduled ? DemoSlots()[0].EndUtc : null,
            DemoSlots(),
            [DemoMessage()],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            "sanne.devries@mail.nl",
            new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            null);
}
