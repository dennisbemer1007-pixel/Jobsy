using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2eFlow;

[Collection("PlaywrightSmoke")]
public sealed class CandidateEmployerFlowE2ePlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_CHECK"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Full_flow_check_with_screenshots_and_metrics()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");

        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
                   ?? throw new InvalidOperationException("ConnectionStrings__JobsyDb required");

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        if (!await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl))
        {
            throw new InvalidOperationException("Feature flags did not turn on.");
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var runner = new E2eFlowRunner(baseUrl, conn);
        await runner.RunAsync(browser);
    }
}
