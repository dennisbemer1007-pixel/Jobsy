namespace Jobsy.Tests;

public class RegisterWizardUiTests
{
    [Fact]
    public void Register_wizard_has_breadcrumbs_address_hint_and_no_activation_link()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Register.razor"));
        Assert.Contains("class=\"register-crumbs\"", razor);
        Assert.Contains("Register.CrumbPage", razor);
        Assert.Contains("Register.AddressHint", razor);
        Assert.Contains("Animate=\"false\"", razor);
        Assert.Contains("HostEnvironment.IsDevelopment()", razor);
        Assert.Contains("private string _kvkNumber = \"\";", razor);
        Assert.DoesNotContain("private string _kvkNumber = \"12345678\";", razor);
        Assert.DoesNotContain("Register.OpenActivationLink", razor);
        Assert.DoesNotContain("Register.ActivationLinkHint", razor);
        Assert.DoesNotContain("_activationUrl", razor);
        Assert.Contains("RegisterEstablishmentIdentity", razor);
        Assert.Contains("Register.KvkDetailsHint", razor);
        Assert.DoesNotContain("@e.Address · @e.KvkEstablishmentId", razor);
        Assert.DoesNotContain("SBI @string.Join", razor);
    }

    [Fact]
    public void Establishment_identity_hides_kvk_id_and_sbi_behind_info_button()
    {
        var identity = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Web/Components/RegisterEstablishmentIdentity.razor"));
        Assert.Contains("register-choice__address-row", identity);
        Assert.Contains("Register.KvkDetailsHint", identity);
        Assert.Contains("Register.EstablishmentNumber", identity);
        Assert.Contains("Register.SbiCodes", identity);
        Assert.Contains("@onclick:stopPropagation=\"true\"", identity);
        Assert.DoesNotContain("@Item.KvkEstablishmentId", identity);
        Assert.DoesNotContain("SBI @", identity);
    }

    [Fact]
    public void Register_css_keeps_confirm_label_visible_and_logo_static()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".wizard-actions {\n    display: flex;\n    flex-wrap: wrap;", css);
        Assert.Contains("min-width: 11rem;", css);
        Assert.Contains("white-space: nowrap;", css);
        Assert.Contains(".register-crumbs {", css);
        Assert.Contains(".register-choice__address-row {", css);
        Assert.Contains(".register-choice__kvk-details {", css);
        Assert.DoesNotContain("register-mascot-bob", css);
        Assert.DoesNotContain("animation: register-mascot-bob", css);
    }

    [Fact]
    public void Production_asset_query_is_cache_busted_and_commit_is_exposed()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains("css/app.min.css?v=20260920-culturefit", app);
        Assert.DoesNotContain("css/app.min.css?v=20260920-fitabs", app);
        Assert.DoesNotContain("css/app.min.css?v=20260920-fit\"", app);
        Assert.DoesNotContain("css/app.min.css?v=20260920-pdfprompt", app);
        Assert.DoesNotContain("css/app.min.css?v=20260920-beroep", app);
        Assert.DoesNotContain("css/app.min.css?v=20260904-ui", app);
        Assert.DoesNotContain("css/app.min.css?v=20260916-prod", app);
        Assert.Contains("name=\"lobsy-commit\"", app);
        Assert.Contains("RENDER_GIT_COMMIT", app);

        var api = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/Program.cs"));
        Assert.Contains("status = \"ok\"", api);
        Assert.Contains("RENDER_GIT_COMMIT", api);
    }

    [Fact]
    public void Register_address_strings_exist_in_all_languages()
    {
        string[] languages = ["nl", "en", "pl", "ro", "ar"];
        string[] keys =
        [
            "Register.Breadcrumb",
            "Register.CrumbPage",
            "Register.AddressLabel",
            "Register.AddressHint",
            "Register.KvkDetailsHint",
            "Register.EstablishmentNumber",
            "Register.SbiCodes"
        ];

        foreach (var key in keys)
        {
            foreach (var lang in languages)
            {
                var value = Jobsy.Web.Localization.UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.NotEqual(key, value);
            }
        }
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
