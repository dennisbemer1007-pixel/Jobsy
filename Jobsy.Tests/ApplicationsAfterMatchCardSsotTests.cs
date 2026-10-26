namespace Jobsy.Tests;

public sealed class ApplicationsAfterMatchCardSsotTests
{
    [Fact]
    public void Sollic_demo_ensure_does_not_purge_all_allowlisted_externals_on_load()
    {
        var root = TestRepo.FindRoot();
        var seeder = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "Ops", "TestAccountsSollicitatiesDemoSeeder.cs"));
        Assert.DoesNotContain(
            "PurgeStaleExternalVacanciesForAcceptatieDemoLoginsAsync",
            seeder[(seeder.IndexOf("TryEnsureForAcceptatieAsync", StringComparison.Ordinal)..seeder.IndexOf("public static Task<User?> FindAcceptatieDemoCandidateAsync", StringComparison.Ordinal))],
            StringComparison.Ordinal);
    }

    [Fact]
    public void Acceptatie_demo_ensure_runs_only_when_list_empty_or_sel_missing()
    {
        var root = TestRepo.FindRoot();
        var data = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrData.cs"));
        Assert.Contains("apps.Count == 0", data, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "await TryEnsureAcceptatieSollicitatiesDemoViaWebAsync(\n                api,\n                configuration,\n                hostEnvironment,\n                candidateEmail,\n                cancellationToken);\n        }\n\n        var rows = new List<SollicitationRow>();",
            data,
            StringComparison.Ordinal);
    }

    [Fact]
    public void After_match_rows_skip_timeline_card_hint()
    {
        var root = TestRepo.FindRoot();
        var data = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrData.cs"));
        Assert.Contains("afterMatch is not null", data, StringComparison.Ordinal);
        Assert.Contains("? null", data, StringComparison.Ordinal);
        Assert.Contains("BuildCardHint", data, StringComparison.Ordinal);
    }

    [Fact]
    public void After_match_cards_use_detail_actions_primary_label()
    {
        var root = TestRepo.FindRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsSsrShells.cs"));
        Assert.Contains("BuildDetailActions", shells, StringComparison.Ordinal);
        Assert.Contains("detail.PrimaryLabel", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void Banenkaart_prefetch_skips_isochrones_off_map()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-prefetch.js"));
        Assert.Contains("onBanenkaart", js, StringComparison.Ordinal);
    }
}
