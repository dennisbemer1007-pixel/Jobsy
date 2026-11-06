using System.Globalization;
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
        Assert.Equal("Naaldwijk", model.City);
        Assert.False(model.IncludeFullAddress);
        Assert.Null(model.WorkplaceLatitude);
        Assert.Null(model.ReachTravelMinutes);
        Assert.Null(model.DistanceKm);
        Assert.Equal(30, model.MaxTravelMinutes);
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
        Assert.False(model.HasUploadedOwnCv);
    }

    [Fact]
    public async Task Render_marks_uploaded_own_cv_on_lobsy_pdf()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            AboutMe: "Ik werk graag met mensen.");
        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            null,
            false,
            prefs,
            null,
            null,
            DateTime.UtcNow,
            hasUploadedOwnCv: true);

        Assert.True(model.HasUploadedOwnCv);
        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
    }

    [Fact]
    public void File_name_and_header_use_the_amsterdam_date()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
        var prefs = new CandidatePreferencesDto(Roles: [], MaxTravelMinutes: 20, PreferredTransport: "Fiets");
        var utc = new DateTime(2026, 10, 3, 23, 30, 0, DateTimeKind.Utc);
        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate", null, null, false, prefs, null, null, utc);
        var name = service.BuildFileName(model);
        Assert.Contains("20261004", name, StringComparison.Ordinal);
        Assert.DoesNotContain("20261003", name, StringComparison.Ordinal);
    }

    [Fact]
    public void Education_line_does_not_repeat_the_direction()
    {
        Assert.Equal("MBO 2 – Logistiek", LobsyCvPdfService.FormatEducation(["MBO 2 – Logistiek"], "Logistiek"));
        Assert.Equal("MBO 2 · Logistiek", LobsyCvPdfService.FormatEducation(["MBO 2"], "Logistiek"));
        Assert.Equal("Logistiek", LobsyCvPdfService.FormatEducation([], "Logistiek"));
    }

    [Fact]
    public async Task Render_does_not_add_whoami_page_when_model_carries_story()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 20,
            PreferredTransport: "Fiets",
            AboutMe: "Ik werk graag met mensen.");
        var who = new LobsyCvWhoAmI(
            "ZZ-AI-MARKER-WHOAMI Ik werk graag samen en houd ritme in de ploeg.",
            ["samenwerken", "rust en ritme"],
            [new LobsyCvScoreBar("samenwerken", 88)],
            [new LobsyCvScoreBar("mensen meenemen", 80)]);
        var plain = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            null,
            false,
            prefs,
            null,
            null,
            DateTime.UtcNow);
        var model = plain with { WhoAmI = who };
        Assert.NotNull(model.WhoAmI);
        var pdf = await service.RenderAsync(model);
        var without = await service.RenderAsync(plain);
        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
        using var withDoc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        using var withoutDoc = UglyToad.PdfPig.PdfDocument.Open(without);
        Assert.Equal(withoutDoc.NumberOfPages, withDoc.NumberOfPages);
        var text = string.Join('\n', withDoc.GetPages().Select(p => p.Text));
        Assert.DoesNotContain("ZZ-AI-MARKER-WHOAMI", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Wie ben ik", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Persoonsprofiel bijgevoegd", text, StringComparison.Ordinal);
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
        Assert.Equal("Naaldwijk", model.City);
        Assert.Null(model.Latitude);
        Assert.Null(model.Longitude);
        Assert.Null(model.ReachTravelMinutes);
        Assert.Single(model.Certificates);

        var maps = new TrackingMapImages();
        var service = new LobsyCvPdfService(new FakeCompanySettings(), maps);
        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal(0, maps.CallCount);
        Assert.Equal(0, maps.ReachCallCount);
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        var text = string.Join('\n', doc.GetPages().Select(p => p.Text));
        Assert.Contains("Naaldwijk", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Voorstraat", text, StringComparison.Ordinal);
        Assert.DoesNotContain("2671", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Live_cv_shows_languages_highlights_and_consent_date()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 25,
            PreferredTransport: "Fiets",
            HomeAddress: "2671 AA Naaldwijk",
            DutchLevel: "vloeiend",
            SpokenLanguages: [new CandidateLanguageDto("en", "goed")],
            EducationDirection: "Logistiek",
            Educations: ["MBO 2"]);
        var highlights = LobsyCvHighlightLines.Build(
            new CompetencyScores(90, 40, 30, 20, 10),
            new RiasecScores(90, 38, 31, 88, 81, 100),
            culture: null,
            values: null);
        var accepted = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var model = LobsyCvModelFactory.FromLiveProfile(
            "Sanne",
            "sanne@test.local",
            null,
            false,
            prefs,
            null,
            null,
            accepted,
            PrivacyConstants.CurrentConsentVersion,
            testHighlights: highlights,
            consentAcceptedAt: accepted);

        Assert.Equal("Naaldwijk", model.City);
        Assert.Contains("Nederlands (vloeiend)", model.Languages!);
        Assert.Contains("Engels (goed)", model.Languages!);
        Assert.Equal("Logistiek", model.EducationDirection);
        Assert.DoesNotContain(PrivacyConstants.CurrentConsentVersion, model.TestHighlights!);

        // A parallel test can leave a non-Gregorian culture on the thread (ar-SA uses Hijri).
        // The PDF must still print the consent date on the Gregorian calendar.
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var arabic = CultureInfo.GetCultureInfo("ar-SA");
        CultureInfo.CurrentCulture = arabic;
        CultureInfo.CurrentUICulture = arabic;
        byte[] pdf;
        try
        {
            var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
            pdf = await service.RenderAsync(model);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        var text = string.Join('\n', doc.GetPages().Select(p => p.Text));
        Assert.Contains("Talen", text, StringComparison.Ordinal);
        Assert.Contains("Engels", text, StringComparison.Ordinal);
        Assert.Contains("Uit je tests", text, StringComparison.Ordinal);
        Assert.Contains("Samenwerken", text, StringComparison.Ordinal);
        Assert.Contains("03-10-2026", text, StringComparison.Ordinal);
        Assert.DoesNotContain("22-04-1448", text, StringComparison.Ordinal);
        Assert.DoesNotContain(PrivacyConstants.CurrentConsentVersion, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Voorstraat", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Live_profile_uses_default_motivation_and_workplace_reach_only_with_employer()
    {
        var prefs = new CandidatePreferencesDto(
            Roles: [],
            MaxTravelMinutes: 25,
            PreferredTransport: "Fiets",
            DefaultMotivation: "Ik wil graag in de horeca werken.",
            HomeAddress: "Geheimstraat 1");

        var withoutEmployer = LobsyCvModelFactory.FromLiveProfile(
            "Ada",
            "ada@test.local",
            null,
            false,
            prefs,
            52.0,
            4.2,
            DateTime.UtcNow);

        Assert.Equal("Ik wil graag in de horeca werken.", withoutEmployer.Motivation);
        Assert.Null(withoutEmployer.ReachTravelMinutes);
        Assert.Null(withoutEmployer.WorkplaceLatitude);

        var withEmployer = LobsyCvModelFactory.FromLiveProfile(
            "Ada",
            "ada@test.local",
            null,
            false,
            prefs,
            52.0,
            4.2,
            DateTime.UtcNow,
            workplaceLatitude: 51.99,
            workplaceLongitude: 4.21,
            workplaceAddress: "Bedrijfsweg 1",
            distanceKm: 3.2,
            estimatedTravelMinutes: 12);

        Assert.Equal(12, withEmployer.ReachTravelMinutes);
        Assert.Equal(3.2, withEmployer.DistanceKm);
        Assert.Equal(51.99, withEmployer.WorkplaceLatitude);
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
            workplaceAddress: "Industrieweg 1, Naaldwijk",
            distanceKm: 2.4);

        Assert.Null(model.Address);
        Assert.Null(model.Latitude);
        Assert.Equal("Industrieweg 1, Naaldwijk", model.WorkplaceAddress);
        Assert.Equal(5, model.ReachTravelMinutes);
        Assert.Equal(2.4, model.DistanceKm);
        Assert.Equal("Graag bij jullie starten", model.Motivation);

        var pdf = await service.RenderAsync(model);
        Assert.True(pdf.Length > 500);
        Assert.Equal(1, maps.ReachCallCount);
        Assert.Equal(0, maps.CallCount);
        Assert.Equal(2400, maps.LastRadiusMeters, 1);
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
    public async Task Employer_cv_pdf_does_not_print_a_match_percent()
    {
        var service = new LobsyCvPdfService(new FakeCompanySettings(), new FakeMapImages());
        var prefs = new CandidatePreferencesDto(
            Roles: ["horeca"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets");
        var model = LobsyCvModelFactory.FromLiveProfile(
            "Ada Candidate",
            "ada@test.local",
            null,
            false,
            prefs,
            null,
            null,
            DateTime.UtcNow,
            vacancyTitle: "Kas medewerker",
            companyName: "Tuinbouw Test",
            matchPercent: 77);
        var pdf = await service.RenderAsync(model);
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdf);
        var text = string.Join('\n', doc.GetPages().Select(p => p.Text));
        Assert.Contains("Kas medewerker", text, StringComparison.Ordinal);
        Assert.DoesNotContain("% match", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("77%", text, StringComparison.Ordinal);
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
        public double LastRadiusMeters { get; private set; }

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
            LastRadiusMeters = radiusMeters;
            return Task.FromResult<byte[]?>(null);
        }
    }
}
