using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Applications;

/// <summary>Deterministic multi-thread hub for after-match demo employer berichten (mockup 11).</summary>
internal static class AfterMatchEmployerMessagesDemoHub
{
    private static readonly Guid WaitApp = Guid.Parse("77777771-0001-4000-8000-000000000001");
    private static readonly Guid Verkoop4790App = Guid.Parse("77777771-0006-4000-8000-000000000006");
    private static readonly Guid NotNowApp = Guid.Parse("77777771-0005-4000-8000-000000000005");

    internal static bool IsDemoApplication(Guid applicationId) =>
        applicationId == WaitApp
        || applicationId == Guid.Parse("77777771-0002-4000-8000-000000000002")
        || applicationId == Guid.Parse("77777771-0003-4000-8000-000000000003")
        || applicationId == Verkoop4790App
        || applicationId == Guid.Parse("77777771-0004-4000-8000-000000000004")
        || applicationId == NotNowApp;

    internal static IReadOnlyList<AfterMatchEmployerMessagesSsrData.ThreadRow> Augment(
        IReadOnlyList<AfterMatchEmployerMessagesSsrData.ThreadRow> loaded,
        Guid contextApplicationId)
    {
        if (!IsDemoApplication(contextApplicationId))
        {
            return loaded;
        }

        var byId = loaded.ToDictionary(r => r.Thread.ApplicationId);
        foreach (var row in BuildDemoRows())
        {
            byId.TryAdd(row.Thread.ApplicationId, row);
        }

        return byId.Values
            .OrderByDescending(r =>
            {
                var last = r.Thread.Messages.Count > 0 ? r.Thread.Messages[^1] : null;
                return last?.CreatedAtUtc
                       ?? r.Thread.AcceptedAtUtc
                       ?? r.Application.CreatedAt;
            })
            .ToList();
    }

    private static IEnumerable<AfterMatchEmployerMessagesSsrData.ThreadRow> BuildDemoRows()
    {
        yield return Row(
            WaitApp,
            "Sanne de Vries",
            true,
            InterviewStatus.Proposing,
            null,
            null,
            [
                new AfterMatchMessageDto(
                    Guid.NewGuid(),
                    true,
                    "Jij",
                    "Hoi Sanne, leuk dat je wilt komen kennismaken! Heb je misschien een certificaat van je hygiënecursus? Geen probleem als dat er niet is.",
                    new DateTime(2026, 10, 14, 7, 12, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc),
                    ApplicationMessageBlockReason.None,
                    false),
            ]);

        var fri = AmsterdamSlot(2026, 10, 23, 9, 0);
        yield return Row(
            Verkoop4790App,
            null,
            false,
            InterviewStatus.Scheduled,
            fri,
            fri.AddMinutes(30),
            [],
            unread: 1,
            vacancyTitle: "Verkoopmedewerker",
            displayLabel: "Kandidaat #4790");

        yield return Row(
            NotNowApp,
            "Yusuf Demir",
            true,
            InterviewStatus.Done,
            null,
            null,
            [],
            outcome: AfterMatchOutcomeKind.Offer,
            vacancyTitle: "Bezorger");
    }

    private static AfterMatchEmployerMessagesSsrData.ThreadRow Row(
        Guid appId,
        string? candidateName,
        bool pii,
        InterviewStatus status,
        DateTime? scheduledStart,
        DateTime? scheduledEnd,
        IReadOnlyList<AfterMatchMessageDto> messages,
        int unread = 0,
        AfterMatchOutcomeKind? outcome = null,
        string vacancyTitle = "Medewerker bakkerij",
        string? displayLabel = null)
    {
        var item = new EmployerApplicationItem
        {
            Id = appId,
            VacancyTitle = vacancyTitle,
            CompanyName = "Bakkerij De Korenaar",
            CandidateName = displayLabel ?? candidateName,
            Status = "Accepted",
            PiiRevealed = pii,
            CreatedAt = new DateTime(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc),
        };

        var threadName = displayLabel ?? candidateName ?? "Kandidaat";
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            status,
            AfterMatchContactShareState.From(status, null),
            outcome,
            vacancyTitle,
            "Bakkerij De Korenaar",
            candidateName is null ? null : "sanne.devries@mail.nl",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            scheduledStart,
            scheduledEnd,
            [],
            messages,
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            threadName,
            null,
            new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc),
            null);

        if (unread > 0 && messages.Count == 0)
        {
            thread = thread with
            {
                Messages =
                [
                    new AfterMatchMessageDto(
                        Guid.NewGuid(),
                        false,
                        threadName,
                        "Hoi, ik kan vrijdag om 09:00.",
                        new DateTime(2026, 10, 15, 14, 0, 0, DateTimeKind.Utc),
                        null,
                        ApplicationMessageBlockReason.None,
                        false),
                ],
            };
        }

        return new AfterMatchEmployerMessagesSsrData.ThreadRow(item, thread);
    }

    private static DateTime AmsterdamSlot(int y, int m, int d, int hour, int minute)
        => Jobsy.Core.Time.AmsterdamTime.ToUtc(new DateTime(y, m, d, hour, minute, 0, DateTimeKind.Unspecified));
}
