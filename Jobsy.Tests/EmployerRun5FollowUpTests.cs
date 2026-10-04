using System.Reflection;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Core.Admin;
using Jobsy.Core.Interfaces;
using Jobsy.Web.Admin;
using Jobsy.Web.Help;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>Employer run 5 follow-ups: Bewijzen editor, apply steps, save status, talentpool help, audit.</summary>
public class EmployerRun5FollowUpTests
{
    [Fact]
    public void Passport_proof_editor_is_not_the_mobile_filter_sheet()
    {
        var root = FindRoot();
        var tab = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Passport/PassportProofTab.razor"));
        Assert.Contains("class=\"passport-proof__sheet\"", tab, StringComparison.Ordinal);
        Assert.Contains("class=\"passport-proof__backdrop\"", tab, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"passport-proof-sheet\"", tab, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"passport-proof-save\"", tab, StringComparison.Ordinal);
        Assert.Contains("data-jobsy-dialog-close", tab, StringComparison.Ordinal);
        Assert.Contains("jobsyDialog.trap", tab, StringComparison.Ordinal);
        Assert.Contains("e.Key == \"Escape\"", tab, StringComparison.Ordinal);
        Assert.DoesNotContain("filter-sheet-backdrop", tab, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"filter-sheet ", tab, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"filter-sheet\"", tab, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/mijn-paspoort.css"));
        Assert.Contains(".passport-proof__sheet", css, StringComparison.Ordinal);
        Assert.Contains(".passport-proof__backdrop", css, StringComparison.Ordinal);
        Assert.Contains(".app-shell:has(.passport-proof__sheet) .app-main", css, StringComparison.Ordinal);
        Assert.Contains(".app-shell:has(.passport-proof__sheet) .bottom-nav", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 100", css, StringComparison.Ordinal);

        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".filter-sheet-backdrop,\n    .filter-sheet {\n        display: none !important;", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Talentpool_help_matches_the_page_intro_without_riasec()
    {
        var doc = PageHelpDocs.TryGet("/werkgever/talentpool");
        Assert.NotNull(doc);
        Assert.Equal(
            "Zoek kandidaten op reistijd, beschikbaarheid en rijbewijs — zonder leeftijdsfilter.",
            doc!.Purpose);
        Assert.DoesNotContain("RIASEC", doc.Purpose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("competentie", doc.Purpose, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_email_step_is_only_shown_when_the_vacancy_asks_for_it()
    {
        var detail = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("RequiresEmailCode => _vacancy?.RequireEmailVerification == true", detail, StringComparison.Ordinal);
        Assert.Contains("apply-steps--three", detail, StringComparison.Ordinal);
        Assert.Contains("Apply.LeadReadyThree", detail, StringComparison.Ordinal);
        Assert.Contains("@if (RequiresEmailCode)", detail, StringComparison.Ordinal);

        var hintAt = detail.IndexOf("Apply.EmailCodeHint", StringComparison.Ordinal);
        var verifyAt = detail.IndexOf("Apply.StepVerify", StringComparison.Ordinal);
        Assert.True(hintAt > 0 && verifyAt > 0);
        Assert.Contains("RequiresEmailCode", detail[..hintAt], StringComparison.Ordinal);
        Assert.Contains("RequiresEmailCode", detail[..verifyAt], StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/wwwroot/css/features/kandidaat-banen.css"));
        Assert.Contains("ol.apply-steps.apply-steps--three", css, StringComparison.Ordinal);
        Assert.Contains("repeat(3, minmax(0, 1fr))", css, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nl", "In 3 korte stappen. Duidelijk en snel op je telefoon.")]
    [InlineData("en", "In 3 short steps. Clear and quick on your phone.")]
    [InlineData("pl", "W 3 krótkich krokach. Jasno i szybko na telefonie.")]
    [InlineData("ro", "În 3 pași scurți. Clar și rapid pe telefon.")]
    [InlineData("ar", "في 3 خطوات قصيرة. واضح وسريع على هاتفك.")]
    public void Three_step_apply_lead_exists_in_every_locale(string lang, string expected)
        => Assert.Equal(expected, UiStrings.Get("Apply.LeadReadyThree", lang));

    [Fact]
    public void Passport_data_save_uses_one_status_region()
    {
        var tab = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/Components/Candidate/Passport/PassportDataTab.razor"));
        Assert.Contains("<ProfileSaveBar ShowMessage=\"false\" />", tab, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", tab, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", tab, StringComparison.Ordinal);
        Assert.Equal(1, Count(tab, "role=\"status\""));
        Assert.DoesNotContain("profile-save-bar__message", tab, StringComparison.Ordinal);
    }

    [Fact]
    public void Toggling_EmployersEnabled_is_collected_as_instelling_gewijzigd()
    {
        var method = typeof(SettingsController).GetMethod(
            "CollectPlatformFeatureChanges",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var before = new PlatformFeatureSnapshot(true, false, "https://lobsy.nl", DateTime.UtcNow);
        foreach (var field in new[] { "EmployersEnabled", "CandidatePassportEnabled" })
        {
            var after = Clone(before);
            var prop = typeof(PlatformFeatureSnapshot).GetProperty(field);
            Assert.NotNull(prop);
            prop!.SetValue(after, !(bool)prop.GetValue(before)!);
            var changes = (System.Collections.IEnumerable)method!.Invoke(null, [before, after])!;
            var fields = changes.Cast<object>()
                .Select(row => row.GetType().GetField("Item1")!.GetValue(row)?.ToString())
                .ToList();
            Assert.Contains(field, fields);
        }

        var label = AdminActionLabels.Label(
            AdminAuditKeys.SettingsPlatformUpdate,
            key => key == "AdminAudit.Action.Setting" ? "Instelling gewijzigd" : key);
        Assert.Equal("Instelling gewijzigd", label);

        var controller = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Api/Controllers/SettingsController.cs"));
        Assert.Contains("Reason: request.Reason", controller, StringComparison.Ordinal);
        Assert.Contains("Add(\"EmployersEnabled\"", controller, StringComparison.Ordinal);
        Assert.Contains("Add(\"CandidatePassportEnabled\"", controller, StringComparison.Ordinal);
    }

    private static int Count(string text, string needle)
    {
        var n = 0;
        var i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            n++;
            i += needle.Length;
        }

        return n;
    }

    private static PlatformFeatureSnapshot Clone(PlatformFeatureSnapshot source)
        => JsonSerializer.Deserialize<PlatformFeatureSnapshot>(JsonSerializer.Serialize(source))!;

    private static string FindRoot()
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
