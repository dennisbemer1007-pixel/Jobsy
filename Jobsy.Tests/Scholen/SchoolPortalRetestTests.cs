using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class SchoolPortalRetestTests
{
    [Fact]
    public void Pupil_school_picker_listens_on_document_so_prerender_cannot_drop_it()
    {
        var root = RepoRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "leerling-login.js"));
        Assert.Contains("document.addEventListener(\"change\"", js, StringComparison.Ordinal);
        Assert.Contains("select[data-leerling-school]", js, StringComparison.Ordinal);
        Assert.DoesNotContain("DOMContentLoaded", js, StringComparison.Ordinal);
        Assert.DoesNotContain("dataset.bound", js, StringComparison.Ordinal);
    }

    [Fact]
    public void Class_file_name_keeps_spaces_and_slashes_as_hyphens()
    {
        Assert.Equal("ZZ-Test-7-8", SchoolPortalService.SanitizeFile("ZZ Test 7/8"));
        Assert.Equal("klas", SchoolPortalService.SanitizeFile("///"));
    }

    [Fact]
    public void Portal_action_error_hides_raw_problem_json()
    {
        var raw = """{"title":"Interne serverfout","detail":"Er ging iets mis.","supportCode":"LB-1"}""";
        Assert.Equal(PortalActionError.Fallback, PortalActionError.From(raw));
        Assert.Equal("Vul een school-e-mail in.", PortalActionError.From("Vul een school-e-mail in."));
    }

    [Fact]
    public void Durations_come_from_one_source()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsScholen.MergeNl(nl);
        Assert.Contains(PupilSessionDuration.Groep78, nl["Leerling.Start.NoteTime"], StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.VoLessons, nl["Leerling.Vo.Start.NoteTime"], StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.MboHbo, nl["Leerling.MboHbo.Start.NoteTime"], StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.Groep78, nl["School.Material.Step4"], StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.VoLessons, nl["School.Material.Step4"], StringComparison.Ordinal);
        Assert.Contains("(100 vragen)", nl["School.QuestionSet.Vo"], StringComparison.Ordinal);
        Assert.DoesNotContain("25 minuten", nl["Leerling.Start.NoteTime"], StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.Groep78, OuderbriefTemplate.Groep78CountLine, StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.VoLetter, OuderbriefTemplate.VoCountLine, StringComparison.Ordinal);

        var root = RepoRoot();
        var lesbrief = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "docs", "scholen", "lesbrief-lobsy.html"));
        Assert.Contains(PupilSessionDuration.Groep78, lesbrief, StringComparison.Ordinal);
        Assert.Contains(PupilSessionDuration.VoLessons, lesbrief, StringComparison.Ordinal);
        Assert.DoesNotContain("on-demand", lesbrief, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("niet vooraf gegenereerd", lesbrief, StringComparison.OrdinalIgnoreCase);
        var materials = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Scholen", "SchoolPortalSsrHtml.cs"));
        Assert.DoesNotContain("OnDemandNote", materials, StringComparison.Ordinal);
    }

    [Fact]
    public void School_and_pupil_razor_has_no_raw_inline_handlers_or_local_time()
    {
        var root = RepoRoot();
        var folders = new[]
        {
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leraar"),
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Leerling"),
            Path.Combine(root, "Jobsy.Web", "Components", "School")
        };
        var inline = new Regex(@"(?<!@)\bon[a-z]+\s*=\s*[""']");
        var hits = new List<string>();
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                if (inline.IsMatch(text))
                {
                    hits.Add(Path.GetRelativePath(root, file));
                }

                if (text.Contains("ToLocalTime(", StringComparison.Ordinal))
                {
                    hits.Add(Path.GetRelativePath(root, file) + " ToLocalTime");
                }
            }
        }

        Assert.True(hits.Count == 0, string.Join(Environment.NewLine, hits));
    }

    private static string RepoRoot()
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

public class SchoolResultsInitTests : JobsyBunitContext

{
    public SchoolResultsInitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Results_ssr_shows_class_error_without_per_code_table()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var classes = new List<SchoolPortalClassListItemDto>
        {
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "7A",
                SchoolLevel.Groep78,
                7,
                2026,
                "2026-2027",
                [],
                1,
                0,
                true,
                TestWindowState.Open,
                null,
                PupilQuestionSet.Groep78)
        };
        var html = SchoolPortalSsrHtml.Results(
            classes,
            PupilQuestionSet.Groep78,
            false,
            classes[0].Id,
            null,
            culture["School.Results.ClassFailed"],
            culture);
        Assert.Contains("De resultaten van deze klas konden niet geladen worden", html, StringComparison.Ordinal);
        Assert.DoesNotContain("sch-code", html, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
