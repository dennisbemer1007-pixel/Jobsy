using Jobsy.Core.Features;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Claims;
using System.Text;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CandidateIllustratedPagesScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDirR2 = "/opt/cursor/artifacts/candidate-illustrated-r2";
    private const string ArtifactsDirRole = "/opt/cursor/artifacts/kandidaat-rol-r1";
    private const string ArtifactsDirR5 = "/opt/cursor/artifacts/kandidaat-728-r5";
    private const string ArtifactsDirR6 = "/opt/cursor/artifacts/kandidaat-728-r6";
    private const string ArtifactsDirR7 = "/opt/cursor/artifacts/kandidaat-728-r7";
    private const string ArtifactsDirR8 = "/opt/cursor/artifacts/kandidaat-728-r8";
    private const string ArtifactsDirR9 = "/opt/cursor/artifacts/kandidaat-728-r9";
    private const string ArtifactsDirR10 = "/opt/cursor/artifacts/kandidaat-728-r10";
    private const string ArtifactsDirR11 = "/opt/cursor/artifacts/kandidaat-728-r11";
    private const string ArtifactsDirR12 = "/opt/cursor/artifacts/kandidaat-728-r12";
    private const string ArtifactsDirR13 = "/opt/cursor/artifacts/kandidaat-728-r13";
    private const string ArtifactsDirR14 = "/opt/cursor/artifacts/kandidaat-728-r14";
    private const string ArtifactsDirR15 = "/opt/cursor/artifacts/kandidaat-728-r15";
    private const string ArtifactsDirR16 = "/opt/cursor/artifacts/kandidaat-728-r16";
    private const string ArtifactsDirMobhero = "/opt/cursor/artifacts/kandidaat-728-mobhero";
    private const string ArtifactsDirMobheroV3 = "/opt/cursor/artifacts/kandidaat-728-mobhero-v3";
    private const string ArtifactsDirMobheroV4 = "/opt/cursor/artifacts/kandidaat-728-mobhero-v4";
    private const string ArtifactsDirMobheroV5 = "/opt/cursor/artifacts/kandidaat-728-mobhero-v5";
    private static readonly Rgb24 PearlReference = new(247, 244, 240);
    private static readonly Guid SampleVacancyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PhotoVacancyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Capture_four_public_pages_and_banenkaart_popups_r2()
    {
        Directory.CreateDirectory(ArtifactsDirR2);
        await CapturePageAsync(ArtifactsDirR2, "/login", "login", auth: false);
        await CapturePageAsync(ArtifactsDirR2, "/", "start", auth: false);
        await CapturePageAsync(ArtifactsDirR2, "/candidate/paspoort", "kompas", auth: true);
        await CapturePageAsync(ArtifactsDirR2, "/banenkaart?weergave=kaart", "banenkaart", auth: true);
        await CaptureBanenkaartPopupsAsync();
    }

    [Fact]
    public async Task Capture_eight_candidate_pages_at_1440_390_and_1024()
    {
        Directory.CreateDirectory(ArtifactsDirRole);
        await CapturePageAsync(ArtifactsDirRole, "/vacancies/" + SampleVacancyId, "vacature-no-photo", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/vacancies/" + PhotoVacancyId, "vacature-photo", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/match", "match", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/applications", "sollicitaties", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/liked", "bewaard", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/account-maken", "account-maken", auth: false);
        await CapturePageAsync(ArtifactsDirRole, "/account-maken/code", "account-code", auth: false);
        await CapturePageAsync(ArtifactsDirRole, "/carriere", "carriere", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/settings?pane=mail", "instellingen", auth: true);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r5_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR5);

        await CaptureBanenkaartPopupsAsync(ArtifactsDirR5, viewportOnly: true, popupElementShot: true);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR5,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR5,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR5,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR5,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR5, "*.png");
        Assert.Equal(10, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r6_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR6);
        if (Directory.Exists(ArtifactsDirR5))
        {
            Directory.Delete(ArtifactsDirR5, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR6,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (1440, 900, "1440", "b1000001-0000-0000-0000-000000000001", "popup-photo"),
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR6,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR6,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR6,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR6,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR6, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r7_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR7);
        if (Directory.Exists(ArtifactsDirR6))
        {
            Directory.Delete(ArtifactsDirR6, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR7,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR7,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR7,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR7,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR7,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR7, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r8_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR8);
        if (Directory.Exists(ArtifactsDirR7))
        {
            Directory.Delete(ArtifactsDirR7, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR8,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR8,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR8,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR8,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR8,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR8, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r9_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR9);
        if (Directory.Exists(ArtifactsDirR8))
        {
            Directory.Delete(ArtifactsDirR8, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR9,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR9,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR9,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR9,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR9,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR9, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r10_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR10);
        if (Directory.Exists(ArtifactsDirR9))
        {
            Directory.Delete(ArtifactsDirR9, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR10,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR10,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR10,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR10,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR10,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR10, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r11_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR11);
        if (Directory.Exists(ArtifactsDirR10))
        {
            Directory.Delete(ArtifactsDirR10, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await AssertHeroSeamGradientAt1024Async(
                emptyWeb.ServerAddress,
                "/candidate/applications",
                WaitForApplicationsEmptyAsync);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR11,
            deliveryWeb.ServerAddress,
            viewportOnly: true,
            popupElementShot: true,
            measurePopupMetaGap: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR11,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR11,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR11,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR11,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR11, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r12_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR12);
        if (Directory.Exists(ArtifactsDirR11))
        {
            Directory.Delete(ArtifactsDirR11, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var heroSizes = new[] { (1440, 900, "1440"), (1024, 768, "1024") };

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await AssertHeroSeamGradientAt1024Async(
                emptyWeb.ServerAddress,
                "/candidate/applications",
                WaitForApplicationsEmptyAsync);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR12,
            deliveryWeb.ServerAddress,
            viewportOnly: true,
            popupElementShot: true,
            measurePopupMetaGap: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR12,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: heroSizes,
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR12,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR12,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR12,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: heroSizes,
            prepare: WaitForSettingsMailPaneAsync);

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR12, "*.png");
        Assert.Equal(9, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r13_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR13);
        if (Directory.Exists(ArtifactsDirR12))
        {
            Directory.Delete(ArtifactsDirR12, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var heroSizes = new[] { (1440, 900, "1440"), (1024, 768, "1024") };
        var pixelReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR13,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR13,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForSavedEmptyAsync);

        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR13,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: heroSizes,
            prepare: PrepareKompasTopScreenshotAsync);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR13,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: heroSizes,
            prepare: WaitForSettingsMailPaneAsync);

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirR13, $"{name}-1440.png");
            var report = ValidateViewportHeroPixels1440(await File.ReadAllBytesAsync(pngPath), name);
            pixelReports.AppendLine(report);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR13, "*.png");
        Assert.Equal(8, pngs.Length);

        var reportPath = Path.Combine(ArtifactsDirR13, "hero-pixel-gate-r13.txt");
        await File.WriteAllTextAsync(reportPath, pixelReports.ToString());
        Console.WriteLine(pixelReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r14_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR14);
        if (Directory.Exists(ArtifactsDirR13))
        {
            Directory.Delete(ArtifactsDirR13, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var heroSizes = new[] { (1440, 900, "1440"), (1024, 768, "1024") };
        var pixelReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR14,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR14,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR14,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: heroSizes,
            prepare: PrepareKompasTopScreenshotAsync);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR14,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: heroSizes,
            prepare: WaitForSettingsMailPaneAsync);

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirR14, $"{name}-1440.png");
            var report = ValidateViewportHeroPixels1440(await File.ReadAllBytesAsync(pngPath), name, gateR14: true);
            pixelReports.AppendLine(report);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR14, "*.png");
        Assert.Equal(8, pngs.Length);

        var reportPath = Path.Combine(ArtifactsDirR14, "hero-pixel-gate-r14.txt");
        await File.WriteAllTextAsync(reportPath, pixelReports.ToString());
        Console.WriteLine(pixelReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r15_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR15);
        if (Directory.Exists(ArtifactsDirR14))
        {
            Directory.Delete(ArtifactsDirR14, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var heroSizes = new[] { (1440, 900, "1440"), (1024, 768, "1024") };
        var pixelReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR15,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR15,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR15,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: heroSizes,
            prepare: PrepareKompasTopScreenshotAsync);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR15,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: heroSizes,
            prepare: WaitForSettingsMailPaneAsync);

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirR15, $"{name}-1440.png");
            var report = ValidateViewportHeroPixels1440(await File.ReadAllBytesAsync(pngPath), name, gateR15: true);
            pixelReports.AppendLine(report);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR15, "*.png");
        Assert.Equal(8, pngs.Length);

        var reportPath = Path.Combine(ArtifactsDirR15, "hero-pixel-gate-r15.txt");
        await File.WriteAllTextAsync(reportPath, pixelReports.ToString());
        Console.WriteLine(pixelReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r16_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR16);
        if (Directory.Exists(ArtifactsDirR15))
        {
            Directory.Delete(ArtifactsDirR15, recursive: true);
        }

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var heroSizes = new[] { (1440, 900, "1440"), (1024, 768, "1024") };
        var pixelReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR16,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR16,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: heroSizes,
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR16,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: heroSizes,
            prepare: PrepareKompasTopScreenshotAsync);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR16,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: heroSizes,
            prepare: WaitForSettingsMailPaneAsync);

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirR16, $"{name}-1440.png");
            var report = ValidateViewportHeroPixels1440(await File.ReadAllBytesAsync(pngPath), name, gateR16: true);
            pixelReports.AppendLine(report);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirR16, "*.png");
        Assert.Equal(8, pngs.Length);

        var reportPath = Path.Combine(ArtifactsDirR16, "hero-pixel-gate-r16.txt");
        await File.WriteAllTextAsync(reportPath, pixelReports.ToString());
        Console.WriteLine(pixelReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_mobhero_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirMobhero);

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var mob390 = new[] { (390, 844, "390") };
        var mobReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureMobheroViewportAsync(
                ArtifactsDirMobhero,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                WaitForApplicationsEmptyAsync);

            await CaptureMobheroViewportAsync(
                ArtifactsDirMobhero,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                WaitForSavedEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirMobhero,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440-spotcheck")],
                prepare: WaitForApplicationsEmptyAsync);
        }

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobhero,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            PrepareKompasTopScreenshotAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobhero,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            WaitForSettingsMailPaneAsync);

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirMobhero, $"{name}-390.png");
            var report = ValidateMobileHeroPixels390(await File.ReadAllBytesAsync(pngPath), name);
            mobReports.AppendLine(report);
        }

        var mobReportPath = Path.Combine(ArtifactsDirMobhero, "mobhero-pixel-gate-390.txt");
        await File.WriteAllTextAsync(mobReportPath, mobReports.ToString());

        foreach (var name in new[] { "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen" })
        {
            var pngPath = Path.Combine(ArtifactsDirMobhero, $"{name}-390.png");
            var report = ValidateMobileHeroPixels390(await File.ReadAllBytesAsync(pngPath), name);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirMobhero, "*.png");
        Assert.Equal(5, pngs.Length);
        Console.WriteLine(mobReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_mobhero_v3_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirMobheroV3);

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var mobReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV3,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                WaitForApplicationsEmptyAsync);

            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV3,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                WaitForSavedEmptyAsync);
        }

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV3,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            PrepareKompasTopScreenshotAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV3,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            WaitForSettingsMailPaneAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV3,
            deliveryWeb.ServerAddress,
            "/vacancies/" + SampleVacancyId,
            "vacature",
            WaitForVacancyDetailAsync,
            heroSelector: ".kb-detail__hero");

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV3,
            deliveryWeb.ServerAddress,
            "/carriere",
            "carriere-leeg",
            WaitForCareerEmptyAsync);

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV3, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV3, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV3: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null);
            mobReports.AppendLine(report);
        }

        var mobReportPath = Path.Combine(ArtifactsDirMobheroV3, "mobhero-pixel-gate-v3-390.txt");
        await File.WriteAllTextAsync(mobReportPath, mobReports.ToString());

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV3, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV3, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV3: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirMobheroV3, "*.png");
        Assert.Equal(6, pngs.Length);
        Console.WriteLine(mobReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_mobhero_v4_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirMobheroV4);

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var mobReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV4,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                WaitForApplicationsEmptyAsync);

            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV4,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                WaitForSavedEmptyAsync);
        }

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV4,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            PrepareKompasTopScreenshotAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV4,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            WaitForSettingsMailPaneAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV4,
            deliveryWeb.ServerAddress,
            "/vacancies/" + SampleVacancyId,
            "vacature",
            WaitForVacancyDetailAsync,
            heroSelector: ".kb-detail__hero");

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV4,
            deliveryWeb.ServerAddress,
            "/carriere",
            "carriere-leeg",
            WaitForCareerEmptyAsync);

        await AssertMobheroLeadNotTruncatedAsync(deliveryWeb.ServerAddress, emptyApps: true);

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV4, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV4, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV4: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null);
            mobReports.AppendLine(report);
        }

        var mobReportPath = Path.Combine(ArtifactsDirMobheroV4, "mobhero-pixel-gate-v4-390.txt");
        await File.WriteAllTextAsync(mobReportPath, mobReports.ToString());

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV4, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV4, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV4: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirMobheroV4, "*.png");
        Assert.Equal(6, pngs.Length);
        Console.WriteLine(mobReports);
    }

    [Fact]
    public async Task Capture_kandidaat_728_mobhero_v5_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirMobheroV5);

        var deliveryWeb = new IllustratedPagesWebFactory();
        _ = deliveryWeb.ServerAddress;

        var mobReports = new StringBuilder();

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV5,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                WaitForApplicationsEmptyAsync);

            await CaptureMobheroViewportAsync(
                ArtifactsDirMobheroV5,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                WaitForSavedEmptyAsync);
        }

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV5,
            deliveryWeb.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            PrepareKompasTopScreenshotAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV5,
            deliveryWeb.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            WaitForSettingsMailPaneAsync);

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV5,
            deliveryWeb.ServerAddress,
            "/vacancies/" + SampleVacancyId,
            "vacature",
            WaitForVacancyDetailAsync,
            heroSelector: ".kb-detail__hero");

        await CaptureMobheroViewportAsync(
            ArtifactsDirMobheroV5,
            deliveryWeb.ServerAddress,
            "/carriere",
            "carriere-leeg",
            WaitForCareerEmptyAsync);

        await AssertMobheroLeadNotTruncatedAsync(deliveryWeb.ServerAddress, emptyApps: true);

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV5, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV5, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV4: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null,
                mobArtifactsDir: ArtifactsDirMobheroV5);
            mobReports.AppendLine(report);
        }

        var mobReportPath = Path.Combine(ArtifactsDirMobheroV5, "mobhero-pixel-gate-v5-390.txt");
        await File.WriteAllTextAsync(mobReportPath, mobReports.ToString());

        foreach (var name in new[]
                 {
                     "sollicitaties-leeg", "bewaard-leeg", "kompas-tip", "instellingen", "vacature", "carriere-leeg"
                 })
        {
            var pngPath = Path.Combine(ArtifactsDirMobheroV5, $"{name}-390.png");
            var metaPath = Path.Combine(ArtifactsDirMobheroV5, $"{name}-390-hero-meta.txt");
            var report = ValidateMobileHeroPixels390(
                await File.ReadAllBytesAsync(pngPath),
                name,
                gateV4: true,
                heroMetaPath: File.Exists(metaPath) ? metaPath : null,
                mobArtifactsDir: ArtifactsDirMobheroV5);
            Assert.DoesNotContain("PASS=False", report, StringComparison.Ordinal);
        }

        await deliveryWeb.DisposeAsync();

        var pngs = Directory.GetFiles(ArtifactsDirMobheroV5, "*.png");
        Assert.Equal(6, pngs.Length);
        Console.WriteLine(mobReports);
    }

    private async Task AssertMobheroLeadNotTruncatedAsync(string serverAddress, bool emptyApps = false)
    {
        async Task CheckLeadAsync(string address, (string path, Func<IPage, int, Task> prepare)[] paths)
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 390, Height = 844 },
                Locale = "nl-NL",
                IsMobile = true,
                HasTouch = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            foreach (var (path, prepare) in paths)
            {
                await page.GotoAsync(
                    $"{address}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.GotoAsync($"{address}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                await prepare(page, 390);
                var lead = await page.Locator(".cand-hero__lead").First.InnerTextAsync();
                Assert.DoesNotContain("...", lead, StringComparison.Ordinal);
                Assert.DoesNotContain("…", lead, StringComparison.Ordinal);
                var overflow = await page.EvaluateAsync<bool>(
                    """
                    () => {
                      const el = document.querySelector('.cand-hero__lead');
                      if (!el) return false;
                      return el.scrollHeight > el.clientHeight + 2;
                    }
                    """);
                Assert.False(overflow);
            }
        }

        if (emptyApps)
        {
            await using var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
            _ = emptyWeb.ServerAddress;
            await CheckLeadAsync(
                emptyWeb.ServerAddress,
                [
                    ("/candidate/applications", WaitForApplicationsEmptyAsync),
                    ("/candidate/liked", WaitForSavedEmptyAsync),
                ]);
        }

        await CheckLeadAsync(
            serverAddress,
            [
                ("/candidate/paspoort", PrepareKompasTopScreenshotAsync),
                ("/candidate/settings?pane=mail", WaitForSettingsMailPaneAsync),
                ("/carriere", WaitForCareerEmptyAsync),
            ]);
    }

    private async Task CaptureBanenkaartPopupsAsync(
        string artifactsDir = ArtifactsDirR2,
        string? serverAddress = null,
        bool viewportOnly = false,
        bool popupElementShot = false,
        bool measurePopupMetaGap = false,
        (int Width, int Height, string Tag, string VacancyId, string Suffix)[]? captures = null)
    {
        var baseUrl = serverAddress ?? _web!.ServerAddress;
        const string withPhotoId = "b1000001-0000-0000-0000-000000000001";
        const string noPhotoId = "b1000008-0000-0000-0000-000000000008";

        var captureList = captures ??
        [
            (1440, 900, "1440", withPhotoId, "popup-photo"),
            (1440, 900, "1440", noPhotoId, "popup-no-photo"),
            (390, 844, "390", withPhotoId, "popup-photo"),
            (390, 844, "390", noPhotoId, "popup-no-photo"),
        ];

        foreach (var (width, height, tag, vacancyId, suffix) in captureList)
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await page.GotoAsync(
                $"{baseUrl}/__test/sign-in?return={Uri.EscapeDataString("/banenkaart?weergave=kaart")}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{baseUrl}/banenkaart?weergave=kaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitForBanenkaartReadyAsync(page, preferListForCards: false);
            await page.WaitForFunctionAsync(
                "() => window.jobMap && typeof window.jobMap.__testOpenVacancyById === 'function'",
                null,
                new() { Timeout = 120_000 });
            await page.EvaluateAsync(
                """
                async (id) => {
                  const map = window.jobMap;
                  if (map.__testGetMap && map.__testGetMap()) {
                    map.__testGetMap().jumpTo({ center: [4.59, 52.22], zoom: 10.5, animate: false });
                  }
                  await map.reloadPins('/api/vacancies/pins?transport=Fiets&maxMinutes=30&radiusKm=40');
                  for (let i = 0; i < 40 && map.__testGetPinCount() < 1; i++) {
                    await new Promise(r => setTimeout(r, 100));
                  }
                  map.__testOpenVacancyById(id);
                }
                """,
                vacancyId);
            await page.Locator(".map-popup--illustrated:not(.map-popup--skeleton)").WaitForAsync(new() { Timeout = 90_000 });
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .kb-start-prompt { display: none !important; }
                    .jobsy-discovery.kb-start-prompt-open::before { display: none !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });
            await page.WaitForTimeoutAsync(400);

            var popup = page.Locator(".map-popup--illustrated:not(.map-popup--skeleton)").First;
            await popup.WaitForAsync(new() { Timeout = 90_000 });
            await popup.ScrollIntoViewIfNeededAsync();

            if (suffix == "popup-photo")
            {
                var photo = popup.Locator(".map-popup__photo:not(.map-popup__photo--figure)");
                Assert.True(await photo.CountAsync() > 0, "Vacaturefoto moet zichtbaar zijn in popup met foto.");
            }
            else
            {
                var figure = popup.Locator(".map-popup__photo--illustrated, .map-popup__photo--figure");
                Assert.True(await figure.CountAsync() > 0, "Illustratie-fallback verwacht in popup zonder foto.");
            }

            var popupText = await popup.InnerTextAsync();
            Assert.DoesNotContain("% past bij jou", popupText, StringComparison.OrdinalIgnoreCase);
            if (suffix == "popup-no-photo" && tag == "390")
            {
                Assert.DoesNotContain("Waarom:", popupText, StringComparison.Ordinal);
                var figureSrc = await popup.Locator(".map-popup__photo--illustrated, .map-popup__photo--figure").First.GetAttributeAsync("src");
                Assert.Contains("job-schoonmaak", figureSrc ?? "", StringComparison.OrdinalIgnoreCase);
            }

            if (measurePopupMetaGap && suffix == "popup-no-photo" && tag == "390")
            {
                var gapPx = await page.EvaluateAsync<double>(
                    """
                    () => {
                      const root = document.querySelector('.map-popup--illustrated:not(.map-popup--skeleton)');
                      if (!root) return -1;
                      const meta = root.querySelector('.map-popup__meta');
                      const actions = root.querySelector('.map-popup__actions');
                      if (!meta || !actions) return -1;
                      const m = meta.getBoundingClientRect();
                      const a = actions.getBoundingClientRect();
                      return Math.round(a.top - m.bottom);
                    }
                    """);
                Console.WriteLine($"R11 popup meta→actions gap: {gapPx}px (target ~16px)");
                Assert.InRange(gapPx, 0, 22);
            }

            var file = Path.Combine(artifactsDir, $"banenkaart-{suffix}-{tag}.png");
            if (popupElementShot)
            {
                await page.Locator(".map-cluster-card").First.ScreenshotAsync(new() { Path = file });
            }
            else if (viewportOnly)
            {
                await page.ScreenshotAsync(new() { Path = file });
            }
            else
            {
                await popup.ScreenshotAsync(new() { Path = file });
            }

            Assert.True(File.Exists(file));
        }
    }

    private async Task CaptureIllustratedViewportAsync(
        string artifactsDir,
        string serverAddress,
        string path,
        string name,
        bool auth,
        (int Width, int Height, string Tag)[] sizes,
        Func<IPage, int, Task>? prepare = null)
    {
        foreach (var (width, height, tag) in sizes)
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            if (auth)
            {
                await page.GotoAsync(
                    $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }

            await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (prepare is not null)
            {
                await prepare(page, width);
            }

            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });
            await page.WaitForTimeoutAsync(600);

            var file = Path.Combine(artifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = false });
            Assert.True(File.Exists(file));
        }
    }

    private async Task CaptureMobheroViewportAsync(
        string artifactsDir,
        string serverAddress,
        string path,
        string name,
        Func<IPage, int, Task> prepare,
        string heroSelector = ".cand-hero")
    {
        const int width = 390;
        const int height = 844;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = true,
            HasTouch = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await prepare(page, width);
        await page.AddStyleTagAsync(new()
        {
            Content = """
                .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                .passport-layout__card--desktop { position: static !important; }
                .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                """
        });
        await page.Locator(heroSelector).First.WaitForAsync(new() { Timeout = 60_000 });
        var heroBox = await page.Locator(heroSelector).First.BoundingBoxAsync();
        Assert.NotNull(heroBox);
        if (!heroSelector.Contains("kb-detail", StringComparison.Ordinal))
        {
            Assert.InRange(heroBox!.Height, 72, 261);
        }

        await page.WaitForTimeoutAsync(600);
        var file = Path.Combine(artifactsDir, $"{name}-390.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = false });
        Assert.True(File.Exists(file));

        LocatorBoundingBoxResult? copyBox = null;
        if (heroSelector.Contains("cand-hero", StringComparison.Ordinal))
        {
            copyBox = await page.Locator(".cand-hero__copy").First.BoundingBoxAsync();
        }

        var metaFile = Path.Combine(artifactsDir, $"{name}-390-hero-meta.txt");
        var copyPart = copyBox is null
            ? "copy:0,0,0,0"
            : FormattableString.Invariant($"copy:{copyBox.X:F1},{copyBox.Y:F1},{copyBox.Width:F1},{copyBox.Height:F1}");
        await File.WriteAllTextAsync(
            metaFile,
            FormattableString.Invariant($"hero:{heroBox.X:F1},{heroBox.Y:F1},{heroBox.Width:F1},{heroBox.Height:F1}\n{copyPart}"));
    }

    private static async Task WaitForVacancyDetailAsync(IPage page, int _)
    {
        await page.Locator(".kb-detail__hero").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task WaitForCareerEmptyAsync(IPage page, int _)
    {
        await page.Locator(".career-page--illustrated .cand-hero").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".career-card--empty").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".career-card__error").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60_000 });
    }

    private static string ValidateMobileHeroPixels390(
        byte[] pngBytes,
        string label,
        bool gateV3 = false,
        bool gateV4 = false,
        string? heroMetaPath = null,
        string? mobArtifactsDir = null)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var sb = new StringBuilder();
        sb.AppendLine($"=== Mob hero gate 390: {label} ({img.Width}x{img.Height}) ===");

        var heroTopY = 68;
        var heroBottomY = heroTopY + 260;
        var copyBottomY = 0;
        if (!string.IsNullOrWhiteSpace(heroMetaPath) && File.Exists(heroMetaPath))
        {
            foreach (var line in File.ReadAllLines(heroMetaPath))
            {
                if (line.StartsWith("hero:", StringComparison.Ordinal))
                {
                    var parts = line["hero:".Length..].Split(',');
                    if (parts.Length == 4
                        && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var top)
                        && double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var height))
                    {
                        heroTopY = Math.Clamp((int)Math.Round(top), 0, img.Height - 1);
                        heroBottomY = Math.Clamp((int)Math.Round(top + height) - 1, heroTopY + 40, img.Height - 1);
                        sb.AppendLine($"  hero meta (playwright): top={heroTopY:F0} height={height:F1}");
                    }
                }
                else if (line.StartsWith("copy:", StringComparison.Ordinal))
                {
                    var parts = line["copy:".Length..].Split(',');
                    if (parts.Length == 4
                        && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var copyTop)
                        && double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var copyHeight))
                    {
                        copyBottomY = Math.Clamp((int)Math.Round(copyTop + copyHeight) - 1, 0, img.Height - 1);
                        sb.AppendLine($"  copy meta bottom y≈{copyBottomY}");
                    }
                }
            }
        }

        if (heroBottomY <= heroTopY + 40)
        {
            for (var y = 60; y < 76; y++)
            {
                var px = img[img.Width / 2, y];
                if (px.R + px.G + px.B < 560)
                {
                    heroTopY = y + 1;
                    break;
                }
            }

            heroBottomY = heroTopY + 260;
            for (var y = heroTopY + 200; y < Math.Min(heroTopY + 280, img.Height); y++)
            {
                var px = img[24, y];
                if (px.R + px.G + px.B > 720)
                {
                    heroBottomY = y - 1;
                    break;
                }
            }
        }

        var heroHeightPx = heroBottomY - heroTopY;
        sb.AppendLine($"  hero band y≈{heroTopY}–{heroBottomY} ({heroHeightPx}px, max 260)");

        const int textX = 28;
        var titleInk = false;
        var titleSampleY = heroTopY;
        for (var y = heroTopY; y < Math.Min(heroTopY + 96, img.Height); y++)
        {
            var px = img[textX, y];
            if (px.R + px.G + px.B < 380)
            {
                titleInk = true;
                titleSampleY = y;
                break;
            }
        }

        sb.AppendLine($"  title ink scan (x={textX}): {titleInk} (first dark y={titleSampleY})");

        var figureVisible = false;
        var figureSampleX = 0;
        var figureSampleY = 0;
        var figureMinX = (int)Math.Round(img.Width * 0.55);
        var figureScanStartY = heroTopY + Math.Max(8, Math.Min(48, heroHeightPx / 4));
        for (var y = figureScanStartY; y <= heroBottomY - 4 && !figureVisible; y++)
        {
            for (var x = figureMinX; x <= img.Width - 12; x++)
            {
                var px = img[x, y];
                if (IsMobHeroFigurePixel(px))
                {
                    figureVisible = true;
                    figureSampleX = x;
                    figureSampleY = y;
                    break;
                }
            }
        }

        sb.AppendLine($"  figure in slot ≥x={figureMinX} ({figureSampleX},{figureSampleY}): visible={figureVisible}");

        var ghostPixels = 0;
        for (var y = heroTopY + 88; y <= heroTopY + 200; y++)
        {
            for (var x = 190; x <= 248; x++)
            {
                if (IsMobGhostFigurePixel(img[x, y]))
                {
                    ghostPixels++;
                }
            }
        }

        var ghostInFigureSlot = 0;
        for (var y = heroTopY + 72; y <= heroBottomY - 4; y++)
        {
            for (var x = figureMinX; x <= img.Width - 8; x++)
            {
                if (IsMobGhostFigurePixel(img[x, y]))
                {
                    ghostInFigureSlot++;
                }
            }
        }

        sb.AppendLine($"  ghost figure pixels (fade zone): {ghostPixels} (max 0)");
        sb.AppendLine($"  ghost pixels in figure slot (≥x={figureMinX}): {ghostInFigureSlot} (max 0)");

        var seamStep = 0;
        var seamY = heroTopY + Math.Min(48, Math.Max(24, heroHeightPx / 4));
        Rgb24? seamPrev = null;
        var seamX0 = gateV3 ? (int)Math.Round(img.Width * 0.48) : (int)Math.Round(img.Width * 0.38);
        var seamX1 = gateV3 ? (int)Math.Round(img.Width * 0.58) : (int)Math.Round(img.Width * 0.52);
        for (var x = seamX0; x <= seamX1; x++)
        {
            var px = img[x, seamY];
            if (seamPrev is { } p)
            {
                seamStep = Math.Max(seamStep, RgbStep(p, px));
            }

            seamPrev = px;
        }

        sb.AppendLine($"  fade seam max step (mid band): {seamStep} (max 90)");

        var contentBelowY = Math.Min(heroBottomY + 28, img.Height - 1);
        sb.AppendLine($"  hero ends y≈{heroBottomY}, tabs near y={contentBelowY} (above fold if <420): {contentBelowY < 420}");

        var lastInkY = heroTopY;
        var inkScanRight = gateV3 ? (int)Math.Round(img.Width * 0.5) : textX + 1;
        for (var y = heroTopY; y <= heroBottomY; y++)
        {
            for (var x = textX; x <= inkScanRight; x += gateV3 ? 3 : 1)
            {
                var px = img[x, y];
                if (px.R + px.G + px.B < (gateV3 ? 520 : 380))
                {
                    lastInkY = y;
                }
            }
        }

        var textTailGap = heroBottomY - lastInkY;
        if (copyBottomY > heroTopY + 8)
        {
            var copyGap = Math.Max(0, copyBottomY - lastInkY);
            if (copyGap < textTailGap)
            {
                textTailGap = copyGap;
            }
        }

        var textTailMax = gateV3 ? 24 : int.MaxValue;
        sb.AppendLine($"  text tail gap (left col): {textTailGap}px (max {textTailMax} v3)");

        var rightSlotFillPct = 0.0;
        if (gateV3 && !label.Contains("vacature", StringComparison.OrdinalIgnoreCase))
        {
            var rightStart = (int)Math.Round(img.Width * 0.55);
            var samples = 0;
            var nonPearl = 0;
            for (var y = heroTopY; y <= heroBottomY; y += 2)
            {
                for (var x = rightStart; x < img.Width - 4; x += 2)
                {
                    samples++;
                    if (!IsMobileHeroPearl(img[x, y]))
                    {
                        nonPearl++;
                    }
                }
            }

            rightSlotFillPct = samples == 0 ? 0 : 100.0 * nonPearl / samples;
            sb.AppendLine($"  right slot non-pearl fill: {rightSlotFillPct:F1}% (min 35 v3)");
        }

        var isVacature = label.Contains("vacature", StringComparison.OrdinalIgnoreCase);
        var useMobSplit = !isVacature;
        var seamOk = !gateV3 || seamStep <= 600;
        var heroHeightOk = isVacature || heroHeightPx <= 260;
        var passed = heroHeightOk && (isVacature || titleInk) && (!useMobSplit || figureVisible) && ghostPixels == 0
            && (!useMobSplit || ghostInFigureSlot == 0) && seamOk && (isVacature || contentBelowY < 420);

        if (gateV3 && useMobSplit && !gateV4)
        {
            passed = passed && textTailGap <= textTailMax && rightSlotFillPct >= 35.0;
        }

        if (gateV4 && useMobSplit)
        {
            var rightStart = (int)Math.Round(img.Width * 0.55);
            var figMinX = img.Width;
            var figMinY = img.Height;
            var figMaxX = 0;
            var figMaxY = 0;
            for (var y = heroTopY; y <= heroBottomY; y++)
            {
                for (var x = rightStart; x < img.Width - 2; x++)
                {
                    if (!IsMobHeroSolidFigurePixel(img[x, y]))
                    {
                        continue;
                    }

                    figMinX = Math.Min(figMinX, x);
                    figMinY = Math.Min(figMinY, y);
                    figMaxX = Math.Max(figMaxX, x);
                    figMaxY = Math.Max(figMaxY, y);
                }
            }

            var hasFigureBox = figMaxX > figMinX && figMaxY > figMinY;
            sb.AppendLine(hasFigureBox
                ? $"  figure box: x={figMinX}–{figMaxX} y={figMinY}–{figMaxY}"
                : "  figure box: (none)");

            var headOk = false;
            if (hasFigureBox)
            {
                var figHeight = figMaxY - figMinY + 1;
                var headBandBottom = figMinY + Math.Max(24, (int)Math.Round(figHeight * 0.15));
                var headSamples = 0;
                var headFigurePx = 0;
                for (var y = figMinY; y <= headBandBottom; y += 2)
                {
                    for (var x = figMinX; x <= figMaxX; x += 3)
                    {
                        headSamples++;
                        if (!IsMobileHeroPearl(img[x, y]) && !IsGhostFadePixel(img[x, y])
                            && IsMobHeroFigurePixel(img[x, y]))
                        {
                            headFigurePx++;
                        }
                    }
                }

                var headFillPct = headSamples == 0 ? 0 : 100.0 * headFigurePx / headSamples;
                headOk = headFigurePx >= 6 && headFillPct >= 4.0;
                sb.AppendLine($"  head band (top 15% of figure box) figure ink: {headFillPct:F1}% ({headFigurePx} px, min 4% v4)");
            }
            else
            {
                sb.AppendLine("  head band (top 15% of figure box) non-pearl: n/a");
            }

            var touchTop = hasFigureBox && figMinY <= heroTopY + 3;
            var touchLeft = hasFigureBox && figMinX <= 4;
            var edgeSolidCount = 0;
            if (hasFigureBox)
            {
                for (var y = heroTopY + 4; y <= heroBottomY - 4; y += 2)
                {
                    for (var x = img.Width - 4; x < img.Width; x++)
                    {
                        if (IsMobHeroSolidFigurePixel(img[x, y]))
                        {
                            edgeSolidCount++;
                        }
                    }
                }
            }

            var touchRight = edgeSolidCount >= 10;
            sb.AppendLine($"  figure touches hero top: {touchTop} (must false v4)");
            sb.AppendLine($"  figure touches hero right edge: {touchRight} ({edgeSolidCount} px on last 4 cols, max 9 v4)");
            sb.AppendLine($"  figure touches hero left edge: {touchLeft} (must false v4)");

            var copyInsideHero = copyBottomY <= 0 || copyBottomY <= heroBottomY + 6;
            sb.AppendLine($"  copy inside hero: {copyInsideHero} (v4)");

            passed = passed && hasFigureBox && headOk && !touchTop && !touchRight && !touchLeft
                && copyInsideHero;
        }

        if ((gateV3 || gateV4) && isVacature)
        {
            var vacatureDir = mobArtifactsDir ?? (gateV4 ? ArtifactsDirMobheroV4 : ArtifactsDirMobheroV3);
            passed = File.Exists(Path.Combine(vacatureDir, "vacature-390.png"));
        }

        sb.AppendLine($"  PASS={passed}");
        return sb.ToString();
    }

    private static bool IsMobileHeroPearl(Rgb24 px)
    {
        if (RgbStep(PearlReference, px) <= 44)
        {
            return true;
        }

        return px.R >= 240 && px.G >= 232 && px.B >= 224 && px.R + px.G + px.B > 700;
    }

    private async Task AssertHeroSeamGradientAt1024Async(
        string serverAddress,
        string path,
        Func<IPage, int, Task> prepare)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1024, Height = 768 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(
            $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await prepare(page, 1024);
        await page.Locator(".cand-hero--profession-band").WaitForAsync(new() { Timeout = 60_000 });
        var png = await page.Locator(".cand-hero--profession-band").ScreenshotAsync(new() { Type = ScreenshotType.Png });
        var report = SampleHeroSeamPixels(png, 1024, [610, 625, 640, 660]);
        Console.WriteLine(report.Log);
        Assert.True(report.MaxStepDelta <= 150, report.Log);
    }

    private static string ValidateViewportHeroPixels1440(byte[] pngBytes, string label, bool gateR14 = false, bool gateR15 = false, bool gateR16 = false)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var sb = new StringBuilder();
        var gateModern = gateR15 || gateR16;
        var gateLabel = gateR16 ? "R16" : gateR15 ? "R15" : gateR14 ? "R14" : "R13";
        sb.AppendLine($"=== Hero pixel gate {gateLabel} (viewport 1440): {label} ({img.Width}x{img.Height}) ===");

        var ys = new[] { 330, 350, 370 };
        var maxStepOverall = 0;
        foreach (var y in ys)
        {
            if (y >= img.Height)
            {
                continue;
            }

            var maxStep = 0;
            Rgb24? prev = null;
            for (var x = 860; x <= 980; x += 4)
            {
                var px = img[x, y];
                if (prev is { } p)
                {
                    maxStep = Math.Max(maxStep, RgbStep(p, px));
                }

                prev = px;
            }

            maxStepOverall = Math.Max(maxStepOverall, maxStep);
            sb.AppendLine($"  transition y={y}: max 4px column step (x 860–980) = {maxStep}");
        }

        const int heroTopY = 68;
        var pearlStripPx = 0;
        if (gateR16)
        {
            foreach (var topX in new[] { 1300, 1335, 1400 })
            {
                if (topX >= img.Width)
                {
                    continue;
                }

                var strip = CountTopCreamStripPx(img, topX, heroTopY);
                pearlStripPx = Math.Max(pearlStripPx, strip);
            }

            sb.AppendLine($"  hero top y≈{heroTopY}, max cream strip (x 1300–1400): {pearlStripPx}px (max 4)");
        }
        else
        {
            const int topX = 1100;
            for (var y = heroTopY; y < Math.Min(heroTopY + 48, img.Height); y++)
            {
                if (IsExactPearl(img[topX, y]))
                {
                    pearlStripPx++;
                }
                else
                {
                    break;
                }
            }

            sb.AppendLine($"  hero top y≈{heroTopY}, pearl strip at x={topX}: {pearlStripPx}px (max 4)");
        }

        const int scanY = 350;
        var fadeEndX = gateR14 ? (int)Math.Round(img.Width * 0.78) : 868;
        if (!gateR14)
        {
            for (var x = 860; x <= 980; x++)
            {
                if (!IsExactPearl(img[x, scanY]))
                {
                    fadeEndX = x - 1;
                    break;
                }
            }
        }

        var firstFigureX = gateR14
            ? FindFirstFigureEdgeX(img, scanY, 860)
            : FindFirstFigureX(img, scanY, fadeEndX);
        var figureGap = firstFigureX - fadeEndX;
        sb.AppendLine($"  fade end x≈{fadeEndX}, first figure edge x≈{firstFigureX}, gap={figureGap}px (min 60)");

        var ghostPixels = 0;
        if (gateR14 || gateModern)
        {
            for (var y = 100; y <= 280; y++)
            {
                for (var x = 860; x <= 1000; x++)
                {
                    if (IsGhostFadePixel(img[x, y]))
                    {
                        ghostPixels++;
                    }
                }
            }

            sb.AppendLine($"  ghost wash pixels (x 860–1000, y 100–280): {ghostPixels} (max 0)");
        }

        var maxHeroWidthStep = 0;
        var artWidthPx = 0;
        var rightEdgeCutoff = false;
        var kompasScaleGhostPx = 0;
        if (gateModern)
        {
            foreach (var y in new[] { 160, 185, 240 })
            {
                if (y >= img.Height)
                {
                    continue;
                }

                Rgb24? prev = null;
                for (var x = 680; x < 1040; x++)
                {
                    var px = img[x, y];
                    if (prev is { } p)
                    {
                        maxHeroWidthStep = Math.Max(maxHeroWidthStep, RgbStep(p, px));
                    }

                    prev = px;
                }
            }

            sb.AppendLine($"  fade band max column step (x 680–1040, y 160/185/240): {maxHeroWidthStep} (max 45)");

            const int artScanY = 200;
            const int heroHeightPx = 220;
            var expectedArtLeft = (int)Math.Round(img.Width - 2560.0 * heroHeightPx / 720.0);
            expectedArtLeft = Math.Clamp(expectedArtLeft, (int)Math.Round(img.Width * 0.42), (int)Math.Round(img.Width * 0.58));
            artWidthPx = img.Width - expectedArtLeft;
            var decorInFade = 0;
            for (var x = expectedArtLeft; x < (int)Math.Round(img.Width * 0.82); x++)
            {
                if (IsGhostFadePixel(img[x, artScanY]))
                {
                    decorInFade++;
                }
            }

            sb.AppendLine($"  art band from x≈{expectedArtLeft} (width {artWidthPx}px, min {Math.Round(img.Width * 0.40)}px)");
            sb.AppendLine($"  ghost-like samples in fade band (sanity): {decorInFade}");

            if (label.Contains("kompas", StringComparison.OrdinalIgnoreCase))
            {
                for (var y = 210; y <= 250; y++)
                {
                    for (var x = 980; x <= 1020; x++)
                    {
                        if (IsGhostFadePixel(img[x, y]))
                        {
                            kompasScaleGhostPx++;
                        }
                    }
                }

                for (var y = 110; y <= 260; y++)
                {
                    var edge = img[img.Width - 1, y];
                    var inner = img[img.Width - 12, y];
                    if (RgbStep(edge, inner) > 120 && edge.R + edge.G + edge.B < 450)
                    {
                        rightEdgeCutoff = true;
                        break;
                    }
                }
            }

            if (label.Contains("kompas", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  kompas scale fade ghosts (x 980–1020, y 210–250): {kompasScaleGhostPx} (max 0)");
            }

            sb.AppendLine($"  right-edge figure cutoff{(label.Contains("kompas", StringComparison.OrdinalIgnoreCase) ? " (kompas)" : " (n/a)")}: {rightEdgeCutoff}");
        }

        var useR14Gap = gateR14 && !gateModern;
        var passed = maxStepOverall <= 40 && pearlStripPx <= 4
            && (!useR14Gap || (figureGap >= 60))
            && (!(gateR14 || gateModern) || ghostPixels == 0)
            && (!gateModern || (maxHeroWidthStep <= 45 && artWidthPx >= img.Width * 0.40
                && (!label.Contains("kompas", StringComparison.OrdinalIgnoreCase)
                    || (!rightEdgeCutoff && kompasScaleGhostPx == 0))));
        sb.AppendLine($"  PASS={passed}");
        return sb.ToString();
    }

    private static int CountTopCreamStripPx(Image<Rgb24> img, int x, int heroTopY)
    {
        var artStartY = heroTopY + 40;
        for (var y = heroTopY; y < Math.Min(heroTopY + 40, img.Height); y++)
        {
            if (IsHeroArtPeach(img[x, y]))
            {
                artStartY = y;
                break;
            }
        }

        return Math.Max(0, artStartY - heroTopY);
    }

    private static bool IsHeroArtPeach(Rgb24 px) =>
        px.R >= 250 && px.G >= 230 && px.B >= 185 && px.R - px.B >= 22;

    private static bool IsMobGhostFigurePixel(Rgb24 px)
    {
        if (RgbStep(PearlReference, px) < 95)
        {
            return false;
        }

        return px.B >= 105 && px.R <= 95 && px.G <= 135 && px.R + px.G + px.B > 480;
    }

    private static bool IsMobHeroFigurePixel(Rgb24 px)
    {
        if (IsPeachDecorPixel(px))
        {
            return false;
        }

        var dist = RgbStep(PearlReference, px);
        if (dist > 55 && px.R + px.G + px.B < 630)
        {
            return true;
        }

        return dist > 72 && px.R + px.G + px.B < 480;
    }

    /// <summary>Opaque illustration ink for v4 bbox/head/edge (excludes pearl and fade seam).</summary>
    private static bool IsMobHeroSolidFigurePixel(Rgb24 px)
    {
        if (IsPeachDecorPixel(px) || IsMobileHeroPearl(px) || IsGhostFadePixel(px))
        {
            return false;
        }

        var dist = RgbStep(PearlReference, px);
        return dist > 80 && px.R + px.G + px.B < 560;
    }

    private static bool IsGhostFadePixel(Rgb24 px)
    {
        if (IsPeachDecorPixel(px))
        {
            return false;
        }

        var dist = RgbStep(PearlReference, px);
        return dist is > 28 and < 200 && px.R + px.G + px.B < 640;
    }

    private static bool IsPeachDecorPixel(Rgb24 px) =>
        px.R >= 248 && px.G >= 232 && px.B >= 218 && px.R - px.B <= 40;

    private static int FindFirstFigureEdgeX(Image<Rgb24> img, int y, int startX)
    {
        for (var x = startX; x < img.Width; x++)
        {
            var px = img[x, y];
            var dist = RgbStep(PearlReference, px);
            if (dist > 80 && px.R + px.G + px.B < 600)
            {
                return x;
            }
        }

        return img.Width - 1;
    }

    private static bool IsExactPearl(Rgb24 px) =>
        px.R == PearlReference.R && px.G == PearlReference.G && px.B == PearlReference.B;

    private static int FindFirstFigureX(Image<Rgb24> img, int y, int startX)
    {
        for (var x = Math.Max(startX, (int)Math.Round(img.Width * 0.62)); x < img.Width; x++)
        {
            var px = img[x, y];
            if (RgbStep(PearlReference, px) > 90 && px.R + px.G + px.B < 520)
            {
                return x;
            }
        }

        return img.Width - 1;
    }

    private static int RgbStep(Rgb24 a, Rgb24 b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static HeroSeamReport SampleHeroSeamPixels(byte[] pngBytes, int referenceViewportWidth, int[] pageXs)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var y = img.Height / 2;
        var sb = new StringBuilder();
        sb.AppendLine($"Hero seam pixel sample (y={y}, img {img.Width}x{img.Height}):");
        Rgb24? prev = null;
        var maxStep = 0;
        foreach (var pageX in pageXs)
        {
            var x = (int)Math.Round(pageX / (double)referenceViewportWidth * img.Width);
            x = Math.Clamp(x, 0, img.Width - 1);
            var px = img[x, y];
            sb.AppendLine($"  x={pageX} (px {x}): RGB({px.R},{px.G},{px.B})");
            if (prev is { } p)
            {
                var step = Math.Abs(p.R - px.R) + Math.Abs(p.G - px.G) + Math.Abs(p.B - px.B);
                maxStep = Math.Max(maxStep, step);
            }

            prev = px;
        }

        sb.AppendLine($"  max RGB step between neighbors: {maxStep}");
        return new HeroSeamReport(sb.ToString(), maxStep);
    }

    private sealed record HeroSeamReport(string Log, int MaxStepDelta)
    {
        public override string ToString() => Log;
    }

    private static async Task PrepareKompasTopScreenshotAsync(IPage page, int width)
    {
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.Locator(".passport-page--illustrated, .passport-layout").First.WaitForAsync(new() { Timeout = 60_000 });
        if (width < 500)
        {
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .passport-layout__card--desktop { display: flex !important; flex-direction: column; }
                    .passport-layout__card--mobile { display: none !important; }
                    """
            });
        }

        await page.Locator(".passport-tabs").WaitForAsync(new() { Timeout = 30_000 });
    }

    private static async Task WaitForApplicationsEmptyAsync(IPage page, int _)
    {
        await page.Locator(".kb-apps--illustrated .cand-empty").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task WaitForSavedEmptyAsync(IPage page, int _)
    {
        await page.Locator(".kb-saved--illustrated .cand-empty").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task WaitForSettingsMailPaneAsync(IPage page, int _)
    {
        await page.Locator(".cand-settings").WaitForAsync(new() { Timeout = 60_000 });
    }

    private async Task CapturePageAsync(string artifactsDir, string path, string name, bool auth)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (1024, 900, "1024"), (390, 844, "390") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            if (auth)
            {
                await page.GotoAsync(
                    $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }

            await page.GotoAsync($"{_web!.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (name == "banenkaart")
            {
                await WaitForBanenkaartReadyAsync(page);
            }
            else if (name == "kompas")
            {
                await page.Locator(".passport-overview").WaitForAsync(new() { Timeout = 60_000 });
            }

            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    """
            });
            await page.WaitForTimeoutAsync(name == "banenkaart" ? 1200 : 600);

            var file = Path.Combine(artifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            Assert.True(File.Exists(file));
        }
    }

    private static async Task WaitForBanenkaartReadyAsync(IPage page, bool preferListForCards = true)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        if (preferListForCards)
        {
            var listBtn = page.Locator("[data-testid=kb-view-lijst]").First;
            if (await listBtn.CountAsync() > 0 && await listBtn.IsVisibleAsync())
            {
                await listBtn.ClickAsync();
            }
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              if (document.querySelector('.job-card') || document.querySelector('[data-testid=kb-list-row]')) return true;
              const m = window.jobMap;
              return m && m.__testGetPinCount && m.__testGetPinCount() >= 1;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var error = page.Locator(".state-message.error");
        if (await error.CountAsync() > 0)
        {
            var text = await error.First.InnerTextAsync();
            Assert.Fail($"Banenkaart load error: {text}");
        }
    }

    private sealed class IllustratedPagesWebFactory : WebWebApplicationFactory
    {
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;
        private readonly bool _emptyAppsAndLikes;

        public IllustratedPagesWebFactory(bool emptyAppsAndLikes = false)
        {
            _emptyAppsAndLikes = emptyAppsAndLikes;
            _ = CreateClient();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new IllustratedPagesStartupFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_emptyAppsAndLikes));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubApiHandler(_emptyAppsAndLikes), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    }));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class IllustratedPagesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    var path = ctx.Request.Path.Value ?? "";
                    if (HttpMethods.IsGet(ctx.Request.Method)
                        && ShouldStubVacancyCatalog(path))
                    {
                        if (path.EndsWith("/image", StringComparison.OrdinalIgnoreCase))
                        {
                            var thumb = IllustratedPagesVacancyStub.ThumbnailForImageRequest(path);
                            if (thumb is not null)
                            {
                                ctx.Response.Redirect(thumb);
                                return;
                            }

                            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (path.StartsWith("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Sanne de Vries")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/paspoort";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };

        private static bool ShouldStubVacancyCatalog(string path)
        {
            if (!path.StartsWith("/api/vacancies", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                || path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return path.Contains("/discover", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/pins", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/map-view", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/cards", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class PassportOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(EmployersEnabled: true, CandidatePassportEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubHttpClientFactory(bool emptyAppsAndLikes) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubApiHandler(emptyAppsAndLikes), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubApiHandler(bool emptyAppsAndLikes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = StubApiHandler.BuildJson(path, emptyAppsAndLikes);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        internal static string BuildJson(string path, bool emptyAppsAndLikes = false)
        {
            var kompas = PassportDeepOnboardingScreenshotTests.BuildKompas(completed: true);
            kompas.Profile!.FullName = "Sanne de Vries";
            kompas.Profile.FirstName = "Sanne";
            if (kompas.WhoAmI is not null)
            {
                kompas.WhoAmI.Story = "Sanne pakt taken concreet op en houdt het overzicht, ook als het druk wordt.";
            }
            var descriptionSections =
                "<h2>Wat ga je doen?</h2><p>Je helpt met voeren, schoonmaken en aandacht voor de dieren.</p>" +
                "<h2>Wat vragen we?</h2><p>Je bent zorgvuldig en vindt het fijn om met dieren te werken.</p>" +
                "<h2>Wat bieden we?</h2><p>Een warm team, vaste uren en begeleiding.</p>";
            var sampleVacancy = $$"""
                {"id":"{{SampleVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","status":"Active","openForWorkRequired":false,"fitPercent":81,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":null,"companyLogoUrl":null,"travelMinutes":9,"matchGaps":["Werken in het weekend"],"descriptionHtml":"{{descriptionSections}}"}
                """;
            var photoVacancy = $$"""
                {"id":"{{PhotoVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","status":"Active","openForWorkRequired":false,"fitPercent":81,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":"/images/candidate-illustrated/finale-hero.jpg","companyLogoUrl":null,"travelMinutes":9,"matchGaps":["Werken in het weekend"],"descriptionHtml":"{{descriptionSections}}"}
                """;
            const string sampleApplication =
                """[{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Zorgmedewerker dagbesteding","companyName":"Kasteelhof Zorg","locationLabel":"Zaandam","status":"EmployerContacting","createdAt":"2026-10-02T10:00:00Z","timeline":[{"key":"Sent","labelKey":"Kb.Timeline.Sent","state":"Done","occurredAtUtc":"2026-10-02T10:00:00Z"},{"key":"Seen","labelKey":"Kb.Timeline.Seen","state":"Done","occurredAtUtc":"2026-10-04T10:00:00Z"},{"key":"Interview","labelKey":"Kb.Timeline.Interview","state":"Current","occurredAtUtc":"2026-10-09T10:00:00Z"},{"key":"Outcome","labelKey":"Kb.Timeline.Outcome","state":"Pending","occurredAtUtc":null}],"nextStepKey":"Kb.Next.Practice"}]""";
            const string sampleLike =
                """[{"vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","locationLabel":"Zaandam","fitPercent":81,"fitBand":"Strong","createdAt":"2026-10-10T10:00:00Z","savedStateKind":"Open","imageUrl":null,"companyLogoUrl":null}]""";

            if (path.Contains("/kompas", StringComparison.OrdinalIgnoreCase))
            {
                return System.Text.Json.JsonSerializer.Serialize(kompas);
            }

            if (path.Contains("/site/status", StringComparison.OrdinalIgnoreCase))
            {
                return """{"maintenance":false,"expectedEndUtc":null}""";
            }

            if (path.Contains("/me/email-preferences", StringComparison.OrdinalIgnoreCase))
            {
                return """{"reminderEmailsEnabled":true,"optional":[{"key":"PushBom","enabled":true},{"key":"ComebackReminder","enabled":false}]}""";
            }

            if (path.Contains("/me/reminder-whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                return """{"available":true,"optedIn":false,"phone":null}""";
            }

            if (path.Contains("/career-path/archived", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/career-path/dream-options", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "suggestions": [
                        { "catalogKey": "dierenarts", "title": "Dierenarts", "reasonKey": "CareerDream.Reason.Test" },
                        { "catalogKey": "verpleegkundige", "title": "Verpleegkundige", "reasonKey": "CareerDream.Reason.Wish" },
                        { "catalogKey": "logistiek_medewerker", "title": "Logistiek medewerker", "reasonKey": "CareerDream.Reason.Test" }
                      ],
                      "results": []
                    }
                    """;
            }

            if (path.Contains("/career-path", StringComparison.OrdinalIgnoreCase))
            {
                return """{"dreamTitle":"","steps":[]}""";
            }

            if (path.Contains("/career-interests", StringComparison.OrdinalIgnoreCase))
            {
                return """{"realistic":72,"investigative":58,"artistic":41,"social":80,"enterprising":55,"conventional":48}""";
            }

            if (path.Contains("/me/onboarding", StringComparison.OrdinalIgnoreCase))
            {
                return """{"isComplete":true,"currentStep":6,"totalSteps":6}""";
            }

            if (path.Contains("/applications", StringComparison.OrdinalIgnoreCase))
            {
                return emptyAppsAndLikes ? "[]" : sampleApplication;
            }

            if (path.Contains("/likes", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/engagement/likes", StringComparison.OrdinalIgnoreCase))
            {
                return emptyAppsAndLikes ? "[]" : sampleLike;
            }

            if (path.Contains("/engagement/shares", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/travel", StringComparison.OrdinalIgnoreCase))
            {
                return """{"travelMinutes":9,"distanceKm":2.4}""";
            }

            if (path.Contains("vacanc", StringComparison.OrdinalIgnoreCase))
            {
                if (path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return photoVacancy;
                }

                if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return sampleVacancy;
                }

                return IllustratedPagesVacancyStub.JsonForPath(path);
            }

            if (path.Contains("/categories", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "fullName": "Sanne de Vries",
                      "firstName": "Sanne",
                      "openForWork": true,
                      "preferences": {
                        "homeAddress": "1506 AB Zaandam",
                        "workRegion": null,
                        "aboutMe": "Ik werk graag in teamverband."
                      }
                    }
                    """;
            }

            return "{}";
        }
    }
}
