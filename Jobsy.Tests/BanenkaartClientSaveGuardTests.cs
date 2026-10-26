namespace Jobsy.Tests;

public sealed class BanenkaartClientSaveGuardTests
{
    [Fact]
    public void Save_endpoint_uses_json_result_and_disables_antiforgery()
    {
        var root = TestRepo.FindRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "BanenkaartClientEndpoints.cs"));
        Assert.Contains("BanenkaartClientSaveResult.ExecuteAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("DisableAntiforgery()", endpoints, StringComparison.Ordinal);

        var result = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "BanenkaartClientSaveResult.cs"));
        Assert.Contains("ILogger", result, StringComparison.Ordinal);
        Assert.Contains("SaveJson", result, StringComparison.Ordinal);
        Assert.Contains("MatchesDesiredLikeStateAsync", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Saved_unsave_endpoint_disables_antiforgery()
    {
        var root = TestRepo.FindRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "SavedClientEndpoints.cs"));
        Assert.Contains("DisableAntiforgery()", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRequestValidAsync", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_ssr_exposes_save_diagnostics_attributes()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "match-ssr.js"));
        Assert.Contains("data-lb-status", js, StringComparison.Ordinal);
        Assert.Contains("data-lb-code", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Saved_ssr_keeps_removed_items_hidden_after_filter()
    {
        var root = TestRepo.FindRoot();
        var unsave = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "saved-ssr-unsave.js"));
        var filters = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "saved-ssr-filters.js"));
        Assert.Contains("data-saved-removed", unsave, StringComparison.Ordinal);
        Assert.Contains("data-saved-removed", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_fit_check_sets_query_param_after_post()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "passport-ssr-fit.js"));
        Assert.Contains("searchParams.set(\"check\"", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Banenkaart_prefetch_skips_isochrones_off_map()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-prefetch.js"));
        Assert.Contains("onBanenkaart", js, StringComparison.Ordinal);
    }
}
