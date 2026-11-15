using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

internal static class G78ContrastAbBlockLayoutAssert
{
    private const double TolerancePx = 2.01;

    internal static async Task AssertEqualAsync(IPage page, string context)
    {
        var sizes = await page.EvaluateAsync<string>(
            """
            () => {
              const a = document.querySelector('[data-testid=ll-g78-contrast-a]');
              const b = document.querySelector('[data-testid=ll-g78-contrast-b]');
              if (!a || !b) return JSON.stringify({ ok: false, reason: 'missing nodes' });
              const ra = a.getBoundingClientRect();
              const rb = b.getBoundingClientRect();
              return JSON.stringify({
                ok: true,
                a: { w: ra.width, h: ra.height },
                b: { w: rb.width, h: rb.height }
              });
            }
            """);

        using var doc = System.Text.Json.JsonDocument.Parse(sizes);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean(), context + ": " + sizes);
        var aw = doc.RootElement.GetProperty("a").GetProperty("w").GetDouble();
        var ah = doc.RootElement.GetProperty("a").GetProperty("h").GetDouble();
        var bw = doc.RootElement.GetProperty("b").GetProperty("w").GetDouble();
        var bh = doc.RootElement.GetProperty("b").GetProperty("h").GetDouble();
        Assert.InRange(Math.Abs(aw - bw), 0, TolerancePx);
        Assert.InRange(Math.Abs(ah - bh), 0, TolerancePx);
    }
}
