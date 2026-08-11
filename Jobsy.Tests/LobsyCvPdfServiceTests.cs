using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
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
                new CandidateEmployerHistoryDto("Café Test", "Bediening", 1, "Borden afruimen", "2022-03", null)
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
            PrivacyConstants.CurrentConsentVersion,
            dateOfBirth: new DateOnly(1998, 4, 12));

        Assert.Equal(new DateOnly(1998, 4, 12), model.DateOfBirth);
        Assert.Equal(AgeRules.AgeYearsFromDateOfBirth(new DateOnly(1998, 4, 12)), model.AgeYears);
        Assert.Equal(2, model.Certificates.Count);
        Assert.Null(model.Latitude);
        Assert.Null(model.Address);
        Assert.Null(model.City);
        Assert.False(model.IncludeFullAddress);
        Assert.Equal("2022-03", model.Employers[0].StartMonth);
        Assert.Null(model.Employers[0].EndMonth);
        Assert.Equal("mrt 2022 – heden", LobsyCvModelFactory.FormatEmployerPeriod(
            model.Employers[0].StartMonth, model.Employers[0].EndMonth, model.Employers[0].Years));

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
    public async Task Live_profile_never_includes_candidate_home_on_cv()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            HomeAddress: "Voorstraat 1, 2671 AB Naaldwijk",
            Certificates: [new CandidateCertificateDto("EHBO", 2022)],
            ShowAddressOnCv: true);

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
        Assert.Equal(0, maps.ReachCallCount);
    }

    [Fact]
    public async Task Application_cv_uses_workplace_reach_map_not_candidate_home()
    {
        var maps = new TrackingMapImages();
        var service = new LobsyCvPdfService(new FakeCompanySettings(), maps);
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
            "Fiets",
            5,
            """{"flexibleTimes":true,"minHoursPerWeek":8,"maxHoursPerWeek":20,"slots":{}}""",
            "B",
            "Havo",
            """[{"name":"VCA","year":2021}]""",
            1,
            80,
            "Magazijnmedewerker",
            "Demo BV",
            PrivacyConstants.CurrentConsentVersion,
            DateTime.UtcNow,
            includeFullAddress: true,
            includeContactDetails: true,
            workplaceLatitude: 51.99,
            workplaceLongitude: 4.21,
            workplaceAddress: "Industrieweg 1, Naaldwijk");

        Assert.Null(model.Address);
        Assert.Null(model.Latitude);
        Assert.Equal("Industrieweg 1, Naaldwijk", model.WorkplaceAddress);
        Assert.Equal(5, model.ReachTravelMinutes);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal(1, maps.ReachCallCount);
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
            includeContactDetails: true,
            dateOfBirth: new DateOnly(2000, 1, 15),
            ageYears: 26,
            workplaceLatitude: 52.01,
            workplaceLongitude: 4.22,
            workplaceAddress: "Bedrijfsweg 9, Den Haag");

        Assert.Single(model.Certificates);
        Assert.Equal("VCA", model.Certificates[0].Name);
        Assert.Equal(2021, model.Certificates[0].Year);
        Assert.Equal(new DateOnly(2000, 1, 15), model.DateOfBirth);
        Assert.Equal(26, model.AgeYears);
        Assert.Null(model.Address);
        Assert.Equal("Bedrijfsweg 9, Den Haag", model.WorkplaceAddress);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal('%', (char)pdf[0]);
    }

    [Fact]
    public void Format_employer_period_supports_range_and_legacy_years()
    {
        Assert.Equal("jan 2020 – dec 2021", LobsyCvModelFactory.FormatEmployerPeriod("2020-01", "2021-12"));
        Assert.Equal("mrt 2022 – heden", LobsyCvModelFactory.FormatEmployerPeriod("2022-03", null));
        Assert.Equal("3 jr", LobsyCvModelFactory.FormatEmployerPeriod(null, null, 3));
        Assert.Null(LobsyCvModelFactory.FormatEmployerPeriod(null, null, null));
        Assert.Equal("2020-01", LobsyCvModelFactory.NormalizeMonth("2020-01-15"));
        Assert.Null(LobsyCvModelFactory.NormalizeMonth("bad"));
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

        public Task<byte[]?> RenderWorkplaceReachAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            int width = 640,
            int height = 280,
            byte[]? markerLogoPng = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<byte[]?>(null);
    }

    private sealed class TrackingMapImages : ICandidateMapImageService
    {
        public int CallCount { get; private set; }
        public int ReachCallCount { get; private set; }

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

        public Task<byte[]?> RenderWorkplaceReachAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            int width = 640,
            int height = 280,
            byte[]? markerLogoPng = null,
            CancellationToken cancellationToken = default)
        {
            ReachCallCount++;
            return Task.FromResult<byte[]?>(null);
        }
    }
}
