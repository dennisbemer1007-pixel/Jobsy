using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>Source guards for candidate-tab stability (error boundary recover + panel shells).</summary>
public class CandidateTabStabilityGuardTests
{
    [Fact]
    public void MainLayout_recovers_error_boundary_on_location_change()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/CircuitErrorBoundary.razor"));
        Assert.Contains("_errorBoundary?.Recover()", src);
        Assert.Contains("OnLocationChanged", src);
        Assert.Contains("LogCircuitError", src);
        Assert.Contains("SentrySdk.CaptureException", src);
        Assert.DoesNotContain("ex.Message", src);
        Assert.DoesNotContain("ex.ToString()", src);
        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("CircuitErrorBoundary", layout);
    }

    [Fact]
    public void Heavy_candidate_panels_are_wrapped_in_PanelErrorBoundary()
    {
        var kompas = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Candidate/CandidateKompas.razor"));
        var profile = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("PanelErrorBoundary Name=\"dna\"", kompas);
        Assert.Contains("PanelErrorBoundary Name=\"tests-overview\"", kompas);
        Assert.Contains("PanelErrorBoundary Name=\"role-fit\"", kompas);
        Assert.Contains("PanelErrorBoundary Name=\"onboarding-resume\"", kompas);
        Assert.Contains("PanelErrorBoundary Name=\"matched-vacancies\"", profile);
        var passport = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Candidate/Passport.razor"));
        Assert.Contains("PanelErrorBoundary Name=\"passport-proof\"", passport);
    }

    [Fact]
    public void Panel_error_copy_exists_in_dutch_strings()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Localization/UiStrings.cs"));
        Assert.Contains("Dit onderdeel laadt even niet", src);
        Assert.Contains("[\"Panel.ErrorRetry\"]", src);
    }

    [Fact]
    public void CircuitExceptionLogger_logs_unhandled_inbound_exceptions()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Hosting/CircuitExceptionLogger.cs"));
        Assert.Contains("CreateInboundActivityHandler", src);
        Assert.Contains("SentrySdk.CaptureException", src);
        Assert.Contains("Unhandled Blazor circuit exception", src);
    }

    [Fact]
    public void Render_yaml_has_Sentry_Dsn_for_web_and_api()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "render.yaml"));
        Assert.Equal(4, Regex.Matches(yaml, @"key:\s*Sentry__Dsn").Count);
        Assert.Contains("Circuit__DetailedErrors", yaml);
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
