using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchEmployerWeekAsideRowsTests : BunitContext
{
    public AfterMatchEmployerWeekAsideRowsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    [Fact]
    public void Week_aside_lists_scheduled_peers_in_current_week_without_hardcoded_4790()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var weekStart = InitiativeWeekLimitRules.WeekStartAmsterdam(DateTime.UtcNow);
        var scheduledUtc = weekStart.AddDays(1).AddHours(10);

        var reschedule = new AfterMatchThreadDto(
            AfterMatchDemoIds.RescheduleApp,
            Guid.NewGuid(),
            InterviewStatus.RescheduleRequested,
            AfterMatchContactShareState.From(InterviewStatus.RescheduleRequested, null),
            null,
            "Medewerker bakkerij",
            "Den Haag Centrum",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            scheduledUtc,
            scheduledUtc.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-2),
            null);

        var scheduledPeer = new AfterMatchThreadDto(
            AfterMatchDemoIds.ScheduledApp,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Medewerker bakkerij",
            "Den Haag Centrum",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            scheduledUtc,
            scheduledUtc.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-2),
            null);

        var rows = AfterMatchEmployerWeekAsideRows.Build(culture, reschedule, [reschedule, scheduledPeer]);
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Subtitle.Contains("10:", StringComparison.Ordinal));
        Assert.DoesNotContain(rows, r => r.Title == "Kandidaat #4790");
    }

    [Fact]
    public void Week_aside_omits_proposed_slots_without_confirmed_interview()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var weekStart = InitiativeWeekLimitRules.WeekStartAmsterdam(DateTime.UtcNow);
        var proposedUtc = weekStart.AddDays(2).AddHours(10).AddMinutes(30);

        var proposing = new AfterMatchThreadDto(
            AfterMatchDemoIds.PickSlotsApp,
            Guid.NewGuid(),
            InterviewStatus.Proposing,
            AfterMatchContactShareState.From(InterviewStatus.Proposing, null),
            null,
            "Hulp bakker",
            "Den Haag",
            null,
            InterviewFormat.OnSite,
            null,
            null,
            null,
            null,
            null,
            [new AfterMatchSlotDto(Guid.NewGuid(), proposedUtc, proposedUtc.AddMinutes(30), InterviewSlotProposer.Employer, null, false)],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Roland",
            null,
            DateTime.UtcNow.AddDays(-2),
            null);

        var rows = AfterMatchEmployerWeekAsideRows.Build(culture, proposing, [proposing]);
        Assert.Empty(rows);
    }
}
