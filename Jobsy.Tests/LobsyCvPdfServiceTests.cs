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
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
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
            Availability: new Dictionary<string, string[]>
            {
                ["Ma"] = ["Ochtend", "Middag"],
                ["Wo"] = ["Avond"]
            },
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 16,
            FlexibleTimes: false,
            HomeAddress: "Voorstraat 1, 2671 AB Naaldwijk",
            Certificates:
            [
                new CandidateCertificateDto("BHV", 2024),
                new CandidateCertificateDto("HACCP", 2023)
            ],
            ShowAddressOnCv: true);

        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            "06 12345678",
            true,
            prefs,
            51.993,
            4.209,
            DateTime.UtcNow,
            PrivacyConstants.CurrentConsentVersion);

        Assert.True(model.IncludeFullAddress);
        Assert.Equal(2, model.Certificates.Count);
        Assert.NotNull(model.Latitude);

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
    public async Task Live_profile_hides_address_and_map_when_opted_out()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            HomeAddress: "Voorstraat 1, 2671 AB Naaldwijk",
            Certificates: [new CandidateCertificateDto("EHBO", 2022)],
            ShowAddressOnCv: false);

        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            "0612345678",
            false,
            prefs,
            51.993,
            4.209,
            DateTime.UtcNow);

        Assert.False(model.IncludeFullAddress);
        Assert.Null(model.Address);
        Assert.Null(model.City);
        Assert.Null(model.Latitude);
        Assert.Null(model.Longitude);
        Assert.Single(model.Certificates);

        var maps = new TrackingMapImages();
        var service = new LobsyCvPdfService(new FakeCompanySettings(), maps);
        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal(0, maps.CallCount);
    }

    [Fact]
    public async Task Render_application_snapshot_includes_vacancy_context()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
        var model = LobsyCvModelFactory.FromApplicationSnapshot(
            "Bert Bijbaan",
            "bert@test.local",
            "0612345678",
            true,
            "Westland",
            "Straat 2, Westland",
            52.0,
            4.2,
            "Hardwerker",
            "Graag bij jullie starten",
            "OV",
            25,
            """{"flexibleTimes":true,"minHoursPerWeek":8,"maxHoursPerWeek":20,"slots":{}}""",
            "B,AM",
            "Havo",
            """[{"name":"VCA","year":2021}]""",
            2,
            72,
            "Magazijnmedewerker",
            "Demo BV",
            PrivacyConstants.CurrentConsentVersion,
            DateTime.UtcNow,
            includeFullAddress: true,
            includeContactDetails: true);

        Assert.Single(model.Certificates);
        Assert.Equal("VCA", model.Certificates[0].Name);
        Assert.Equal(2021, model.Certificates[0].Year);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal('%', (char)pdf[0]);
    }

    [Fact]
    public void Serialize_certificates_snapshot_stays_valid_under_max_length()
    {
        var longName = new string('A', 200);
        var certs = Enumerable.Range(0, 30)
            .Select(i => new CandidateCertificateDto($"{longName}-{i}", 2000 + (i % 20)))
            .ToList();

        var json = LobsyCvModelFactory.SerializeCertificatesSnapshot(certs, maxLength: 4000);
        Assert.True(json.Length <= 4000);

        var parsed = LobsyCvModelFactory.ParseCertificatesJson(json);
        Assert.NotEmpty(parsed);
        Assert.All(parsed, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
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

    private sealed class FakeMapImages : ICandidateMapImageService
    {
        public Task<byte[]?> RenderAsync(
            double latitude,
            double longitude,
            int width = 640,
            int height = 280,
            int zoom = 15,
            byte[]? markerLogoPng = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>(null);
    }

    private sealed class TrackingMapImages : ICandidateMapImageService
    {
        public int CallCount { get; private set; }

        public Task<byte[]?> RenderAsync(
            double latitude,
            double longitude,
            int width = 640,
            int height = 280,
            int zoom = 15,
            byte[]? markerLogoPng = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<byte[]?>(null);
        }
    }
}
