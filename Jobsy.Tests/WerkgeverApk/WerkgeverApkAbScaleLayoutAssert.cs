using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

internal static class WerkgeverApkAbScaleLayoutAssert
{
    public static async Task AssertDotsClearOfCardsAsync(IPage page, int viewportWidth, string? label = null)
    {
        var prefix = label is null ? "" : $"{label}: ";
        Assert.Equal(0, await page.Locator(".wa-ab-bipolar__intro").CountAsync());
        Assert.Equal(1, await page.Locator("#wa-ab-q-title").CountAsync());

        var cardA = page.Locator("[data-testid='wa-ab-contrast-a']");
        var cardB = page.Locator("[data-testid='wa-ab-contrast-b']");
        var dots = page.Locator("[data-testid='wa-ab-scale'] .wa-ab-dot");
        Assert.Equal(5, await dots.CountAsync());

        var boxA = await cardA.BoundingBoxAsync();
        var boxB = await cardB.BoundingBoxAsync();
        Assert.NotNull(boxA);
        Assert.NotNull(boxB);

        var scale = page.Locator("[data-testid='wa-ab-scale']");
        var scaleBox = await scale.BoundingBoxAsync();
        Assert.NotNull(scaleBox);

        var vp = page.ViewportSize
                   ?? throw new InvalidOperationException("ViewportSize is required.");

        for (var i = 0; i < 5; i++)
        {
            var dot = await dots.Nth(i).BoundingBoxAsync();
            Assert.NotNull(dot);
            Assert.True(dot!.Width >= 44, $"{prefix}dot {i} touch target width");
            Assert.True(dot.Height >= 44, $"{prefix}dot {i} touch target height");

            Assert.True(dot.X >= -1, $"{prefix}dot {i} clipped left");
            Assert.True(dot.X + dot.Width <= vp.Width + 1, $"{prefix}dot {i} clipped right");
            Assert.True(dot.Y >= -1, $"{prefix}dot {i} clipped top");
            Assert.True(dot.Y + dot.Height <= vp.Height + 1, $"{prefix}dot {i} clipped bottom");

            if (viewportWidth >= 801)
            {
                Assert.False(HorizontallyOverlaps(dot, boxA!), $"{prefix}dot {i} overlaps A card (horizontal)");
                Assert.False(HorizontallyOverlaps(dot, boxB!), $"{prefix}dot {i} overlaps B card (horizontal)");
            }
            else
            {
                Assert.False(Overlaps(dot, boxA!), $"{prefix}dot {i} overlaps A card");
                Assert.False(Overlaps(dot, boxB!), $"{prefix}dot {i} overlaps B card");
            }
        }

        if (viewportWidth >= 801)
        {
            Assert.True(scaleBox!.Width >= 299, $"{prefix}scale column narrower than 300px");
            Assert.True(scaleBox.X >= boxA!.X + boxA.Width - 1, $"{prefix}scale intrudes on A card");
            Assert.True(scaleBox.X + scaleBox.Width <= boxB!.X + 1, $"{prefix}scale intrudes on B card");
        }
        else
        {
            var dot0 = await dots.First.BoundingBoxAsync();
            var dot4 = await dots.Nth(4).BoundingBoxAsync();
            Assert.NotNull(dot0);
            Assert.NotNull(dot4);
            Assert.True(dot4!.X + dot4.Width <= vp.Width + 1, $"{prefix}fifth dot clipped on mobile");
        }
    }

    private static bool Overlaps(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b)
    {
        const double eps = 0.5;
        return a.X + a.Width > b.X + eps
            && b.X + b.Width > a.X + eps
            && a.Y + a.Height > b.Y + eps
            && b.Y + b.Height > a.Y + eps;
    }

    private static bool HorizontallyOverlaps(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b)
    {
        const double eps = 0.5;
        return a.X + a.Width > b.X + eps && b.X + b.Width > a.X + eps;
    }
}