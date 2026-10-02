using Jobsy.Web.Components.Candidate.Journey;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 05 §2: the stone set and order are fixed in <see cref="CandidateHowStones"/> (D15)
/// and never read from the nav. Pure rules — every flag combination is covered here.
/// </summary>
public class CandidateHowStonesTests
{
    [Fact]
    public void All_flags_on_gives_Dennis_order()
    {
        var stones = CandidateHowStones.Build(employersOn: true, passportOn: true);

        Assert.Equal(
            [
                CandidateHowStoneKind.Discovery,
                CandidateHowStoneKind.Passport,
                CandidateHowStoneKind.Career,
                CandidateHowStoneKind.JobMap,
                CandidateHowStoneKind.Applications
            ],
            stones.Select(s => s.Kind));
        Assert.Equal(
            [
                "/candidate/ontdekkingsreis",
                "/candidate/paspoort",
                "/carriere",
                "/banenkaart",
                "/candidate/applications"
            ],
            stones.Select(s => s.Href));
    }

    [Fact]
    public void Werkgevers_off_drops_the_job_map_and_applications_stones()
    {
        var stones = CandidateHowStones.Build(employersOn: false, passportOn: true);

        Assert.Equal(3, stones.Count);
        Assert.DoesNotContain(CandidateHowStoneKind.JobMap, stones.Select(s => s.Kind));
        Assert.DoesNotContain(CandidateHowStoneKind.Applications, stones.Select(s => s.Kind));
    }

    [Fact]
    public void Passport_off_swaps_the_first_two_stones_for_profile_routes()
    {
        var stones = CandidateHowStones.Build(employersOn: true, passportOn: false);

        Assert.Equal("HowC.Stone.Profile.Title", stones[0].TitleKey);
        Assert.Equal("/candidate/start", stones[0].Href);
        Assert.Equal("HowC.Stone.MyProfile.Title", stones[1].TitleKey);
        Assert.Equal("HowC.Stone.MyProfile.Body", stones[1].BodyKey);
        Assert.Equal("/candidate/profile", stones[1].Href);
    }

    [Fact]
    public void Both_flags_off_leaves_three_profile_and_career_stones()
    {
        var stones = CandidateHowStones.Build(employersOn: false, passportOn: false);

        Assert.Equal(
            ["/candidate/start", "/candidate/profile", "/carriere"],
            stones.Select(s => s.Href));
    }

    [Fact]
    public void A_missing_ontdekkingsreis_route_falls_back_to_the_onboarding_stone()
    {
        var routes = CandidateHowStones.DefaultRoutes
            .Where(r => r != CandidateHowStones.DiscoveryHref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stones = CandidateHowStones.Build(employersOn: true, passportOn: true, routes);

        Assert.Equal("HowC.Stone.Profile.Title", stones[0].TitleKey);
        Assert.Equal("/candidate/start", stones[0].Href);
        Assert.Equal(5, stones.Count);
    }

    [Fact]
    public void An_unknown_route_drops_its_stone_and_reports_it()
    {
        var routes = CandidateHowStones.DefaultRoutes
            .Where(r => r != CandidateHowStones.JobMapHref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dropped = new List<string>();

        var stones = CandidateHowStones.Build(
            employersOn: true,
            passportOn: true,
            routes,
            dropped.Add);

        Assert.Equal(["/banenkaart"], dropped);
        Assert.DoesNotContain(CandidateHowStoneKind.JobMap, stones.Select(s => s.Kind));
        Assert.Equal(4, stones.Count);
    }

    [Fact]
    public void Link_wording_families_follow_the_stone_kind()
    {
        var stones = CandidateHowStones.Build(employersOn: true, passportOn: true);

        Assert.Equal(CandidateHowStoneAction.Progress, stones[0].Action);
        Assert.Equal(CandidateHowStoneAction.Open, stones[1].Action);
        Assert.Equal(CandidateHowStoneAction.Progress, stones[2].Action);
        Assert.Equal(CandidateHowStoneAction.Open, stones[3].Action);
        Assert.Equal(CandidateHowStoneAction.Open, stones[4].Action);
    }

    // ---------- row states ----------

    [Fact]
    public void Now_is_the_first_stone_that_is_not_done()
    {
        var rows = CandidateHowRows.Build(
            CandidateHowStones.Build(employersOn: true, passportOn: true),
            new CandidateJourneySummaryApiModel { DiscoveryDone = true, PassportDone = true });

        Assert.Equal(2, CandidateHowRows.DoneCount(rows));
        Assert.Equal(CandidateHowStoneKind.Career, CandidateHowRows.Target(rows)!.Stone.Kind);
        Assert.Equal([1, 2, 3, 4, 5], rows.Select(r => r.Number));
        Assert.Single(rows, r => r.IsNow);
    }

    [Fact]
    public void A_missing_summary_leaves_every_stone_open()
    {
        var rows = CandidateHowRows.Build(
            CandidateHowStones.Build(employersOn: true, passportOn: true),
            summary: null);

        Assert.Equal(0, CandidateHowRows.DoneCount(rows));
        Assert.True(rows[0].IsNow);
    }

    [Fact]
    public void All_done_has_no_now_row_and_targets_the_last_stone()
    {
        var rows = CandidateHowRows.Build(
            CandidateHowStones.Build(employersOn: true, passportOn: true),
            new CandidateJourneySummaryApiModel
            {
                DiscoveryDone = true,
                PassportDone = true,
                CareerDone = true,
                JobMapDone = true,
                ApplicationsDone = true
            });

        Assert.All(rows, r => Assert.False(r.IsNow));
        Assert.Equal(5, CandidateHowRows.DoneCount(rows));
        Assert.Equal(CandidateHowStoneKind.Applications, CandidateHowRows.Target(rows)!.Stone.Kind);
    }

    /// <summary>The nav is off limits in 05; the order lives in the pure builder instead.</summary>
    [Fact]
    public void The_builder_does_not_reference_the_nav_catalog()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "Jobsy.Web",
            "Components",
            "Candidate",
            "Journey",
            "CandidateHowStones.cs"));

        Assert.DoesNotContain("RoleNavCatalog", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NavItem", source, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }
}
