using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class MarketingFlyerPdfServiceTests
{
    [Fact]
    public async Task Render_defaults_produces_single_page_pdf_with_logo_forward_branding()
    {
        await using var db = CreateDb();
        var settings = new MarketingFlyerSettingsService(db);
        var company = new PlatformCompanySettingsService(db);
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());
        var sut = new MarketingFlyerPdfService(settings, company, features);

        var pdf = await sut.RenderAsync();
        Assert.True(pdf.Length > 800);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal(1, PdfPageCounter.Count(pdf));
    }

    [Fact]
    public async Task Update_and_reset_round_trip_preserves_bullets_and_qr_path()
    {
        await using var db = CreateDb();
        var settings = new MarketingFlyerSettingsService(db);

        var updated = await settings.UpdateAsync(new MarketingFlyerUpdate(
            "Kop test",
            "Sub test",
            "Intro test",
            "Punt één\nPunt twee\n• Punt drie",
            "Gratis tot november",
            "50% korting tot december",
            "CTA titel",
            "CTA body",
            "Scan hier",
            "westland",
            "Footer test"));

        Assert.Equal("Kop test", updated.Headline);
        Assert.Equal(3, updated.BulletPoints.Count);
        Assert.Equal("Punt drie", updated.BulletPoints[2]);
        Assert.Equal("/westland", updated.QrPath);

        var reset = await settings.ResetToDefaultsAsync();
        Assert.Equal(MarketingFlyerSettingsService.DefaultHeadline, reset.Headline);
        Assert.Equal("/register", reset.QrPath);
        Assert.True(reset.BulletPoints.Count >= 8);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
