namespace Jobsy.Tests.E2e;

/// <summary>
/// Required Playwright functional gate (CI sets <c>JOBSY_E2E_FUNCTIONAL_GATE=1</c>).
/// </summary>
public static class FunctionalE2eGate
{
    public static bool IsRequired =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_FUNCTIONAL_GATE"),
            "1",
            StringComparison.Ordinal);

    public static string? TryBaseUrl()
    {
        var url = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    public static async Task<string> RequireReachableBaseUrlAsync()
    {
        var url = TryBaseUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("JOBSY_E2E_BASE_URL is required for the functional E2E gate.");
        }

        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (await SmokeReachability.IsReachableAsync(url))
            {
                return url;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException($"JOBSY_E2E_BASE_URL is not reachable: {url}");
    }

    public static void SkipUnlessGateOrReachable(string? baseUrl)
    {
        if (IsRequired)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }
    }

    public static bool ShouldRun(string? baseUrl)
        => IsRequired || (!string.IsNullOrWhiteSpace(baseUrl) && SmokeReachability.IsReachableAsync(baseUrl).GetAwaiter().GetResult());
}
