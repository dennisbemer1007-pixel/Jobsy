namespace Jobsy.Tests;

/// <summary>
/// Source and CSS contracts for candidate run 6. These run without a browser.
/// </summary>
public class CandidateRun6GuardTests
{
    [Fact]
    public void Coach_dock_is_one_bottom_right_widget_on_the_candidate_shell()
    {
        var root = FindRepoRoot();
        var coach = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyCoach.razor"));
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/MainLayout.razor"));
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        var assistant = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyAssistantChat.razor"));

        Assert.Contains("id=\"lobsy-coach-btn\"", coach, StringComparison.Ordinal);
        Assert.Contains("/images/brand/mascot-coach.webp", coach, StringComparison.Ordinal);
        Assert.Contains("alt=\"\"", coach, StringComparison.Ordinal);
        Assert.Contains("<LobsyCoach", layout, StringComparison.Ordinal);
        Assert.Contains("HideEdgeTab=\"true\"", layout, StringComparison.Ordinal);
        Assert.Contains("Coach.Feedback", assistant, StringComparison.Ordinal);
        Assert.Contains("right: 24px;", css, StringComparison.Ordinal);
        Assert.Contains("bottom: 24px;", css, StringComparison.Ordinal);
        Assert.Contains("width: 64px;", css, StringComparison.Ordinal);
        Assert.Contains("width: 56px;", css, StringComparison.Ordinal);
        Assert.Contains("--bottom-nav-h", css, StringComparison.Ordinal);
        Assert.Contains("--cookie-bar-h", css, StringComparison.Ordinal);
        Assert.Contains("--sticky-footer-h", css, StringComparison.Ordinal);
        Assert.Contains("lobsy-coach-dock--compact", css, StringComparison.Ordinal);
        Assert.Contains("html[data-input=\"keyboard\"] h1[tabindex=\"-1\"]:focus", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Free_unlock_copy_does_not_say_paid_or_mollie_for_test_accounts()
    {
        var root = FindRepoRoot();
        var checkout = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysisCheckout.razor"));
        var deep = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
        var pay = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/DeepTestPaymentService.cs"));
        var strings = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Localization/UiStringsTests.cs"));

        Assert.Contains("DeepPay.OpenedTitle", checkout, StringComparison.Ordinal);
        Assert.Contains("Deep.Rail.OpenedReady", checkout, StringComparison.Ordinal);
        Assert.Contains("DeepPay.PriceFree", checkout, StringComparison.Ordinal);
        Assert.Contains("NeedsWaiver", deep, StringComparison.Ordinal);
        Assert.Contains("!waiverAccepted && !user.IsTestAccount", pay, StringComparison.Ordinal);
        Assert.Contains("Fijn! Je {0} staat voor je klaar.", strings, StringComparison.Ordinal);
        Assert.Contains("Start uitgebreide test: gratis (testaccount)", strings, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}test", strings, StringComparison.Ordinal);
    }

    [Fact]
    public void Seeded_likert_stride_is_not_flat_inside_a_domain_block()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure/Ops/TestAccountsSeedService.cs"));
        Assert.Contains("((i * 3 + (i / 7)) % 5) + 1", src, StringComparison.Ordinal);

        var block = Enumerable.Range(1, 5).Select(i => ((i * 3 + (i / 7)) % 5) + 1).ToArray();
        Assert.True(block.Distinct().Count() >= 3, string.Join(',', block));
    }

    [Fact]
    public void Candidate_binnenkort_uses_the_candidate_shell()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateBinnenkort.razor"));
        var soon = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/WerkgeversBinnenkort.razor"));
        Assert.Contains("@page \"/candidate/binnenkort\"", page, StringComparison.Ordinal);
        Assert.Contains("Authorize(Roles = \"Candidate\")", page, StringComparison.Ordinal);
        Assert.Contains("btn btn-primary", page, StringComparison.Ordinal);
        Assert.Contains("NavigateTo(\"/candidate/binnenkort\"", soon, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Jobsy.sln");
    }
}
