namespace Jobsy.Tests;

public sealed class MatchSsrPageTests
{
    [Fact]
    public void Match_page_is_static_ssr_with_client_swipe_script()
    {
        var page = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "CandidateMatchSsr.razor"));
        var shells = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchSsrShells.cs"));
        var swipe = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchSsrSwipeHtml.cs"));
        Assert.Contains("@page \"/candidate/match\"", page, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", page, StringComparison.Ordinal);
        Assert.Contains("match-ssr.js", shells, StringComparison.Ordinal);
        Assert.Contains("data-match-action", swipe, StringComparison.Ordinal);
        Assert.Contains("BanenkaartSsrQuery.SavePath", shells, StringComparison.Ordinal);
        Assert.Contains("JsonScriptEmbed.Append", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("HtmlEncode(JsonSerializer.Serialize", shells, StringComparison.Ordinal);
        Assert.Contains("match-page__keyboard-hint", swipe, StringComparison.Ordinal);
        Assert.Contains("tabindex=\\\"-1\\\"", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_side_panel_can_render_day_in_life_link()
    {
        var side = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchSsrSideHtml.cs"));
        var resolve = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Match", "MatchOccupationDayResolve.cs"));
        Assert.Contains("day-in-life-link", side, StringComparison.Ordinal);
        Assert.Contains("Day.Link", resolve, StringComparison.Ordinal);
        Assert.Contains("EscoId", File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Models", "SwipeViewModel.cs")), StringComparison.Ordinal);
    }
}
