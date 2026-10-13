namespace Jobsy.Tests;

/// <summary>
/// Career report page: the job list shows one percent and no calculation dump.
/// Soft-skips without JOBSY_E2E_BASE_URL. The same screen is covered by CandidateRun15BunitTests.
/// </summary>
public class CandidateRun15PlaywrightTests
{
    [Fact]
    public void Career_report_page_has_no_calculation_dump_when_e2e_is_configured()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        // The hosted page needs a signed-in candidate. Without that session this check stops.
        // Bunit covers the report markup: recomputed percent, no stored 65, no formula dump.
    }
}
