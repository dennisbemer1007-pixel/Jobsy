using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Bunit;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class PupilGroep78JourneySnapshotTests : JobsyBunitContext

{
    public PupilGroep78JourneySnapshotTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Pause_query_meta_uses_answered_count_not_result_line()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var registry = new PupilQuestionSetRegistry();
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["g78"] = "1",
            ["jn"] = "koraalrif",
            ["ja"] = "4",
            ["jt"] = "60",
        });

        var snapshot = PupilGroep78JourneySnapshot.FromPauseQuery(query, culture, registry, PupilJourneyTheme.ZeeExpeditie);

        Assert.True(snapshot.ShowStrip);
        Assert.Equal("koraalrif", snapshot.NodeKey);
        Assert.Contains("4 van 60", snapshot.MetaLine, StringComparison.Ordinal);
        Assert.DoesNotContain("jouw verhaal", snapshot.MetaLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void In_progress_progress_uses_current_node_not_dit_ben_jij()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var registry = new PupilQuestionSetRegistry();
        var def = registry.Get(PupilQuestionSet.Groep78);
        var progress = new PupilProgressStateDto(
            Guid.NewGuid(),
            "8B",
            "ABC",
            PupilCodeStatus.InProgress,
            4,
            def.Bank.AllItems.Count,
            4,
            0,
            false,
            def.Bank.AllItems[3].Id,
            "koraalrif",
            new Dictionary<string, int> { [def.Bank.AllItems[0].Id] = 4 },
            true,
            false,
            false,
            false,
            false,
            [],
            [],
            null,
            null,
            PupilQuestionSet.Groep78,
            "question",
            null,
            PupilJourneyTheme.ZeeExpeditie,
            DateTime.UtcNow);

        var snapshot = PupilGroep78JourneySnapshot.FromProgress(progress, culture, registry, PupilJourneyTheme.ZeeExpeditie);

        Assert.Equal("koraalrif", snapshot.NodeKey);
        Assert.Contains("4 van 60", snapshot.MetaLine, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
