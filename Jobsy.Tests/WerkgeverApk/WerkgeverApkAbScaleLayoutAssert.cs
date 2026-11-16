using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

internal static class WerkgeverApkAbScaleLayoutAssert
{
    private const double EqualTolerancePx = 2.01;
    private const double GapMinPx = 6;

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

        Assert.InRange(Math.Abs(boxA!.Width - boxB!.Width), 0, EqualTolerancePx);
        Assert.InRange(Math.Abs(boxA.Height - boxB.Height), 0, EqualTolerancePx);

        var scale = page.Locator("[data-testid='wa-ab-scale']");
        var scaleBox = await scale.BoundingBoxAsync();
        Assert.NotNull(scaleBox);

        var cardsRow = page.Locator("[data-testid='wa-ab-contrast-cards']");
        var cardsBox = await cardsRow.BoundingBoxAsync();
        Assert.NotNull(cardsBox);

        var vp = page.ViewportSize
                   ?? throw new InvalidOperationException("ViewportSize is required.");

        // A links, B rechts (of gestapeld op smal scherm).
        if (viewportWidth > 360)
        {
            Assert.True(boxA.X < boxB.X, $"{prefix}A should be left of B");
            Assert.InRange(Math.Abs(boxA.Y - boxB.Y), 0, EqualTolerancePx);
        }
        else
        {
            Assert.True(boxB.Y >= boxA.Y + boxA.Height - 1, $"{prefix}B should be below A when stacked");
        }

        // Schaal onder de kaarten, breedte ≈ kaartenrij.
        Assert.True(scaleBox!.Y >= boxA.Y + boxA.Height - 1 + GapMinPx, $"{prefix}scale should sit below cards");
        Assert.True(scaleBox.Y >= boxB.Y + boxB.Height - 1 + GapMinPx, $"{prefix}scale should sit below B");
        Assert.InRange(scaleBox.X, cardsBox!.X - 2, cardsBox.X + 4);
        Assert.InRange(scaleBox.X + scaleBox.Width, cardsBox.X + cardsBox.Width - 4, cardsBox.X + cardsBox.Width + 2);

        var dot0 = await dots.First.BoundingBoxAsync();
        var dot4 = await dots.Nth(4).BoundingBoxAsync();
        Assert.NotNull(dot0);
        Assert.NotNull(dot4);
        var dot0Center = dot0!.X + dot0.Width / 2;
        var dot4Center = dot4!.X + dot4.Width / 2;
        Assert.True(dot0Center <= scaleBox.X + scaleBox.Width * 0.28, $"{prefix}dot 0 not under A pole");
        Assert.True(dot4Center >= scaleBox.X + scaleBox.Width * 0.72, $"{prefix}dot 4 not under B pole");

        for (var i = 0; i < 5; i++)
        {
            var dot = await dots.Nth(i).BoundingBoxAsync();
            Assert.NotNull(dot);
            Assert.True(dot!.Width >= 44, $"{prefix}dot {i} touch target width");
            Assert.True(dot.Height >= 44, $"{prefix}dot {i} touch target height");

            Assert.True(dot.X >= -1, $"{prefix}dot {i} clipped left");
            Assert.True(dot.X + dot.Width <= vp.Width + 1, $"{prefix}dot {i} clipped right");
            Assert.False(Overlaps(dot, boxA), $"{prefix}dot {i} overlaps A card");
            Assert.False(Overlaps(dot, boxB), $"{prefix}dot {i} overlaps B card");
        }

        var labels = page.Locator(".wa-ab-dots__label");
        Assert.Equal(5, await labels.CountAsync());
        var labelTexts = await labels.AllInnerTextsAsync();
        Assert.Equal(["Zeker A", "Liever A", "Allebei", "Liever B", "Zeker B"], labelTexts);

        var maxLabelHeight = await labels.First.EvaluateAsync<double>(
            "el => Math.max(...Array.from(el.parentElement.querySelectorAll('.wa-ab-dots__label')).map(n => n.getBoundingClientRect().height))");
        Assert.True(maxLabelHeight <= 20, $"{prefix}labels wrapped (height {maxLabelHeight})");

        var selCount = await page.Locator(".wa-ab-dot.sel").CountAsync();
        Assert.Equal(1, selCount);
    }

    private static bool Overlaps(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b)
    {
        const double eps = 0.5;
        return a.X + a.Width > b.X + eps
            && b.X + b.Width > a.X + eps
            && a.Y + a.Height > b.Y + eps
            && b.Y + b.Height > a.Y + eps;
    }
}
