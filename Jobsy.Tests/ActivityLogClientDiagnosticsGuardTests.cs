namespace Jobsy.Tests;

public sealed class ActivityLogClientDiagnosticsGuardTests
{
    [Fact]
    public void Activiteiten_unavailable_supports_acceptatie_data_attributes()
    {
        var root = TestRepo.FindRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Applications", "ApplicationsActiviteitenSsrShells.cs"));
        Assert.Contains("data-lb-status=\"", shells, StringComparison.Ordinal);
        Assert.Contains("data-lb-code=\"", shells, StringComparison.Ordinal);
        Assert.Contains("ActiviteitenAcceptatieDiagnostics", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void Activity_log_uses_ilogger_not_trace_writeline()
    {
        var root = TestRepo.FindRoot();
        var webServices = Path.Combine(root, "Jobsy.Web", "Services");
        Assert.False(File.Exists(Path.Combine(webServices, "ActivityLogApiTrace.cs")));
        var diagnostics = File.ReadAllText(Path.Combine(webServices, "ActivityLogClientDiagnostics.cs"));
        Assert.Contains("LogWarning", diagnostics, StringComparison.Ordinal);
        var apiClient = File.ReadAllText(Path.Combine(webServices, "ApiClient", "JobsyApiClient.Activities.cs"));
        Assert.Contains("ILogger", apiClient, StringComparison.Ordinal);
    }

    [Fact]
    public void Job_search_activity_sync_is_resilient()
    {
        var root = TestRepo.FindRoot();
        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "Services", "JobSearchActivityService.cs"));
        Assert.Contains("job_search_activities_sync_failed", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Sollicitaties_client_pdf_route_is_mapped()
    {
        var root = TestRepo.FindRoot();
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Hosting", "SollicitatiesClientEndpoints.cs"));
        Assert.Contains("/api/sollicitaties-client/activiteiten/period.pdf", endpoints, StringComparison.Ordinal);
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "sollicitaties-activiteiten.js"));
        Assert.Contains("/api/sollicitaties-client/activiteiten/period.pdf", js, StringComparison.Ordinal);
    }
}
