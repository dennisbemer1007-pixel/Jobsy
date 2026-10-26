using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
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
    public void Demo_reschedule_includes_4790_row_when_slot_falls_in_current_week()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var thread = new AfterMatchThreadDto(
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
            null,
            null,
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            DateTime.UtcNow.AddDays(-2),
            null);

        var rows = AfterMatchEmployerWeekAsideRows.Build(culture, thread);
        Assert.True(rows.Count >= 2);
        Assert.Contains(rows, r => r.Title == culture["AfterMatch.Employer.Reschedule.WeekRow4790Name"]);
    }
}
