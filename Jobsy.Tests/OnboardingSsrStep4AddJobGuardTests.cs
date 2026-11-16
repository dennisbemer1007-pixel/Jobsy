namespace Jobsy.Tests;

/// <summary>SSR onboarding steps 2–4 must still bind client interactivity (add job, choice tiles).</summary>
public sealed class OnboardingSsrStep4AddJobGuardTests
{
    [Fact]
    public void Onboarding_client_form_mounts_ssr_steps_2_to_4_for_interactivity()
    {
        var root = TestRepo.FindRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "onboarding-client-form.js"));

        Assert.Contains("var ssrTilesOnly = ssrForm && step >= 2 && step <= 4", js, StringComparison.Ordinal);
        Assert.Contains("mountStep4", js, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "if (ssrForm && step >= 2 && step <= 4) {\n                root.setAttribute(\"data-ob-client-mounted\", \"1\");\n                return;\n            }",
            js,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Kompas_ready_page_enables_mobile_document_scroll_in_css()
    {
        var root = TestRepo.FindRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "onboarding-wizard-v3.css"));
        Assert.Contains("[data-ob-rustig-screen=\"kompas\"]", css, StringComparison.Ordinal);
        Assert.Contains("overflow: visible", css, StringComparison.Ordinal);
    }
}
