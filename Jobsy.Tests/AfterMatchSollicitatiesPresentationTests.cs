using Bunit;
using Jobsy.Core.Enums;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class AfterMatchSollicitatiesPresentationTests : BunitContext
{
    public AfterMatchSollicitatiesPresentationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    [Fact]
    public void List_card_timeline_summary_matches_dossier_for_proposing_open_slots()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var yesAt = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);
        var messageAt = yesAt.AddDays(11);
        var summary = new ApplicationAfterMatchSummary(
            Guid.Parse("77777771-0002-4000-8000-000000000002"),
            InterviewStatus.Proposing.ToString(),
            0,
            3,
            null,
            null,
            yesAt,
            yesAt,
            null,
            false,
            messageAt,
            null,
            null);

        var hint = AfterMatchSollicitatiesPresentation.FormatListCardTimelineSummary(culture, summary);

        Assert.NotNull(hint);
        Assert.Contains("13", hint, StringComparison.Ordinal);
        Assert.Contains(culture["AfterMatch.Candidate.ProposalPending.CardAction"], hint, StringComparison.Ordinal);
        Assert.DoesNotContain("24", hint, StringComparison.Ordinal);
        Assert.DoesNotContain("11 ", hint, StringComparison.Ordinal);
    }
}
