using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class FunctionalE2eArtifacts
{
    internal static async Task CaptureAsync(IPage page, string slug, string? tag = null)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var dir = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")
                  ?? "/opt/cursor/artifacts/functional-sweep";
        Directory.CreateDirectory(dir);
        var suffix = string.IsNullOrWhiteSpace(tag) ? "" : $"-{tag}";
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, $"{slug}{suffix}.png"),
            FullPage = false,
        });
    }
}
