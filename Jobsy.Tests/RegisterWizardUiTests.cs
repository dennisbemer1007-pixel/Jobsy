namespace Jobsy.Tests;

public class RegisterWizardUiTests
{
    [Fact]
    public void Register_wizard_prerenders_search_and_has_no_double_question_mark_error()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Register.razor"));
        Assert.Contains("prerender: true", razor);
        Assert.Contains("RegisterWizard", razor);
        Assert.DoesNotContain("GratisDnaRegisterBox", razor);
        Assert.DoesNotContain("@_error ?? \"", razor);
        Assert.DoesNotContain("?? \"KVK", razor);
    }

    [Fact]
    public void Register_wizard_components_have_no_inline_style_and_use_wa_keys()
    {
        var root = Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Registration");
        foreach (var file in Directory.GetFiles(root, "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("style=\"", text);
        }

        var wizard = File.ReadAllText(Path.Combine(root, "RegisterWizard.razor"));
        Assert.Contains("HostEnvironment.IsDevelopment()", wizard);
        Assert.Contains("WaKvkSearchBox", wizard);
        Assert.DoesNotContain("NavigateTo(AuthRedirects.BanenkaartPath", wizard);
        Assert.DoesNotContain("52.1326", wizard);
        Assert.DoesNotContain("5.2913", wizard);
        // Code-expiry must not auto-redirect after a delay (debounce Task.Delay in search is fine).
        Assert.DoesNotContain("await Task.Delay(1800)", wizard);
        Assert.DoesNotContain("BanenkaartPath, forceLoad: true", wizard);

        var codeStep = File.ReadAllText(Path.Combine(root, "WaStepCode.razor"));
        Assert.DoesNotContain("NavigateTo", codeStep);
        Assert.DoesNotContain("Task.Delay", codeStep);
    }

    [Fact]
    public void Registration_service_never_pins_nl_centre_for_manual_entry()
    {
        var service = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure/Services/CompanyRegistrationService.cs"));
        Assert.DoesNotContain("52.1326", service);
        Assert.DoesNotContain("5.2913", service);
        Assert.Contains("LocationUnknown", service);
        Assert.Contains("CompanyLocationSource", service);
    }

    [Fact]
    public void Wa_public_layout_and_mascot_fallback_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Layout/WaPublicLayout.razor")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Registration/WaMascot.razor")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/werkgever-aanmelding.css")));
        Assert.True(File.Exists(Path.Combine(root, "docs/werkgever-aanmelding-landing-followup.md")));
        Assert.True(File.Exists(Path.Combine(root, "docs/werkgever-aanmelding-followups.md")));
    }

    [Fact]
    public void Wa_strings_exist_in_all_languages()
    {
        string[] languages = ["nl", "en", "pl", "ro", "ar"];
        string[] keys =
        [
            "Wa.Search.Title",
            "Wa.Scope.WholeCompany",
            "Wa.Account.Represent",
            "Wa.Code.Expired",
            "Wa.Done.Title",
            "Wa.Layout.Title"
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

    [Fact]
    public void Production_asset_query_is_cache_busted_and_commit_is_exposed()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        AssetVersions.AssertVersionedRefMatchesManifest(app, "css/app.min.css");
        AssetVersions.AssertVersionedRefMatchesManifest(app, "css/features/werkgever-aanmelding.css");
        Assert.Contains("name=\"lobsy-commit\"", app);
        Assert.Contains("RENDER_GIT_COMMIT", app);
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

        throw new InvalidOperationException("Could not find Jobsy.sln from test base directory.");
    }
}
