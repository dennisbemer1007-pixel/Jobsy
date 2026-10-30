using Bunit;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Applications;

internal static class NdmRescheduleShellFixtures
{
    internal static string BuildHtml(string mode)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(
            new FakeAuth());
        ctx.Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
        var culture = ctx.Services.GetRequiredService<CultureState>();

        var appId = Guid.Parse("b0000001-0000-4000-8000-000000000035");
        var start = new DateTime(2026, 10, 22, 8, 30, 0, DateTimeKind.Utc);
        var thread = new AfterMatchThreadDto(
            appId,
            Guid.NewGuid(),
            InterviewStatus.Scheduled,
            AfterMatchContactShareState.From(InterviewStatus.Scheduled, null),
            null,
            "Administratief medewerker (test)",
            "Testbedrijf Blauw (test)",
            "9 min fietsen",
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag",
            "Linda de Wit",
            null,
            start,
            start.AddMinutes(30),
            [],
            [],
            new AfterMatchReminderPreferenceDto(false, true, true),
            [],
            [],
            "Sanne de Vries",
            null,
            start.AddDays(-9),
            null);

        var data = new AfterMatchRescheduleSsrData.PageData(
            thread,
            "/candidate/applications",
            $"/candidate/applications/{appId:D}/after-match",
            $"/candidate/applications/{appId:D}/after-match?panel=pick-slot",
            "Linda",
            "Lead",
            "Donderdag 22 oktober, 10:30",
            "Op locatie",
            "13 okt",
            "15 okt",
            "22 okt",
            null,
            null,
            $"/candidate/applications/{appId:D}/after-match/wait-ssr");

        var body = AfterMatchRescheduleSsrShells.BuildBody(culture, data, appId, mode);
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/na-de-match.css"));
        return "<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"><style>"
            + css
            + "</style></head><body>"
            + body
            + "</body></html>";
    }

    private sealed class FakeAuth : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(new()));
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
