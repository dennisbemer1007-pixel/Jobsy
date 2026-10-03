using System.Text.Json;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Employer Playwright suites read the public feature-flags endpoint and skip while
/// employers are OFF (decision 20). Candidate journeys stay.
/// </summary>
internal static class EmployersPlaywrightGuard
{
    public static async Task SkipIfEmployersOffAsync(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        if (await EmployersAreOffAsync(baseUrl))
        {
            Assert.Skip("Employers OFF (decision 20)");
        }
    }

    public static async Task<bool> EmployersAreOffAsync(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return false;
        }

        var root = baseUrl.Trim().TrimEnd('/');
        var urls = new[]
        {
            root + "/api/settings/feature-flags",
            root.Replace(":5201", ":5200", StringComparison.Ordinal) + "/api/settings/feature-flags"
        };

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        foreach (var url in urls.Distinct(StringComparer.Ordinal))
        {
            try
            {
                var json = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("employersEnabled", out var flag)
                    && flag.ValueKind == JsonValueKind.False)
                {
                    return true;
                }

                if (doc.RootElement.TryGetProperty("employersEnabled", out flag)
                    && (flag.ValueKind == JsonValueKind.True || flag.ValueKind == JsonValueKind.False))
                {
                    return flag.ValueKind == JsonValueKind.False;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (JsonException)
            {
            }
        }

        return false;
    }
}
