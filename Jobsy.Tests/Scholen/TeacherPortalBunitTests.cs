using System.Security.Claims;
using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Components.Ui.Enterprise;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public class TeacherPortalBunitTests : BunitContext
{
    public TeacherPortalBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void EntKpiCard_renders_label_and_value_for_overview_kpis()
    {
        var cut = Render<EntKpiCard>(ps => ps
            .Add(p => p.Label, "Afgerond")
            .Add(p => p.Value, "19/28")
            .Add(p => p.Delta, "68%")
            .Add(p => p.DeltaTone, "positive"));

        Assert.Contains("Afgerond", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("19/28", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("68%", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Teacher_nav_for_class_exposes_codes_and_group_links()
    {
        var classId = Guid.NewGuid();
        var groups = ScholenNav.TeacherGroupsForClass(classId, [(classId, "2B")]);
        var hrefs = groups.SelectMany(g => g.Items).Select(i => i.Href).ToList();
        Assert.Contains(hrefs, h => h.EndsWith("/codes", StringComparison.Ordinal));
        Assert.Contains(hrefs, h => h.EndsWith("/groep", StringComparison.Ordinal));
        Assert.Contains(hrefs, h => h.EndsWith("/droombanen", StringComparison.Ordinal));
        Assert.Contains(hrefs, h => h.EndsWith("/testvenster", StringComparison.Ordinal));
        Assert.Contains(hrefs, h => h.EndsWith("/materiaal", StringComparison.Ordinal));
    }

    [Fact]
    public void Detail_story_matches_sc_t2_tile_count()
    {
        var renderer = new PupilStoryRenderer();
        var result = new PupilResult
        {
            PupilCodeId = Guid.NewGuid(),
            SchoolClassId = Guid.NewGuid(),
            CompletedAtUtc = DateTime.UtcNow,
            HollandCode = "RIS",
            TopValue = SchwartzValuesCatalog.Connection,
            TopCulture = CulturePersonalityCatalog.PeopleFirst,
            CompetenceScoresJson = """{"samenwerken":70,"resultaatgerichtheid":70,"stressbestendigheid":70,"innovatie":50,"extraversie":50}""",
            RiasecScoresJson = """{"realistic":80,"investigative":70,"artistic":30,"social":60,"enterprising":40,"conventional":40}""",
            ValuesScoresJson = """{"autonomy":40,"connection":80,"achievement":50,"stability":45,"impact":60}""",
            CultureScoresJson = """{"autonomy":40,"informal":40,"collaboration":50,"flexibility":40,"innovation":40,"peopleFirst":80}""",
            ScoringVersion = "t",
            StoryTemplateVersion = "1",
            StoryKeysJson = "[]"
        };
        result.StoryKeysJson = PupilStoryTemplates.Serialize(PupilStoryTemplates.SelectKeys(result, ["dieren"]));
        var story = renderer.Render(result, new PupilProgress { LikesJson = """["dieren"]""" });

        Assert.Equal(4, story.Tiles.Count);
        Assert.Contains(story.Tiles, t => t.ModelKey == "riasec");
        Assert.Equal(PupilCodeStatus.NotStarted, PupilCodeStatus.NotStarted);
        _ = typeof(TeacherCodeDetailDto);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
