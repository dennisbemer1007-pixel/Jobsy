using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class LobsyCvPdfServiceTests
{
    [Fact]
    public async Task Render_live_profile_produces_pdf_bytes()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings());
        var prefs = new CandidatePreferencesDto(
            Roles: ["horeca"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets",
            AboutMe: "Ik zoek een bijbaan in de buurt.",
            DrivingLicenses: ["B"],
            Educations: ["MBO"],
            Employers:
            [
                new CandidateEmployerHistoryDto("Café Test", "Bediening", 1, "Borden afruimen")
            ],
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 16,
            FlexibleTimes: true,
            HomeAddress: "Voorstraat 1, 2671 AB Naaldwijk");

        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            prefs,
            DateTime.UtcNow,
            PrivacyConstants.CurrentConsentVersion);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);

        var fileName = service.BuildFileName(model);
        Assert.StartsWith("Lobsy-CV-AC-", fileName);
        Assert.EndsWith(".pdf", fileName);
        Assert.DoesNotContain("@", fileName);
    }

    [Fact]
    public async Task Render_application_snapshot_includes_vacancy_context()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings());
        var model = LobsyCvModelFactory.FromApplicationSnapshot(
            "Bert Bijbaan",
            "bert@test.local",
            "Westland",
            "Straat 2, Westland",
            "Hardwerker",
            "Graag bij jullie starten",
            "OV",
            25,
            """{"flexibleTimes":true}""",
            "B,AM",
            "Havo",
            2,
            72,
            "Magazijnmedewerker",
            "Demo BV",
            PrivacyConstants.CurrentConsentVersion,
            DateTime.UtcNow,
            includeFullAddress: true,
            includeContactEmail: true);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal('%', (char)pdf[0]);
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Test", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(
            PlatformCompanyUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }
}
