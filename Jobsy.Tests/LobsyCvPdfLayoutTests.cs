using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Jobsy.Tests;

public class LobsyCvPdfLayoutTests
{
    private static readonly DateOnly Birth = new(1994, 5, 12);
    private static readonly DateTime Generated = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Live_profile_pdf_omits_date_of_birth_and_age_but_keeps_contact()
    {
        var service = Service();
        var model = Live("Ada Candidate", "ada@test.local", "06 12345678", includeContact: true);
        var pages = await RenderPages(service, model);

        AssertNoDobOrAge(pages, Birth, model.AgeYears);
        var all = string.Join('\n', pages);
        Assert.Contains("ada@test.local", all, StringComparison.Ordinal);
        Assert.Contains("06 12345678", all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Application_snapshot_pdf_omits_date_of_birth_and_age()
    {
        var service = Service();
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
            12,
            """{"flexibleTimes":false,"minHoursPerWeek":8,"maxHoursPerWeek":16,"slots":{"Ma":["Ochtend"]}}""",
            "B",
            "MBO",
            """[{"name":"VCA","year":2021}]""",
            0,
            null,
            "Magazijnmedewerker",
            "Demo BV",
            PrivacyConstants.CurrentConsentVersion,
            Generated,
            includeFullAddress: false,
            includeContactDetails: true,
            dateOfBirth: Birth,
            ageYears: AgeRules.AgeYearsFromDateOfBirth(Birth));

        Assert.Equal(Birth, model.DateOfBirth);
        Assert.NotNull(model.AgeYears);
        var pages = await RenderPages(service, model);
        AssertNoDobOrAge(pages, Birth, model.AgeYears);
        var all = string.Join('\n', pages);
        Assert.Contains("bert@test.local", all, StringComparison.Ordinal);
        Assert.Contains("0612345678", all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uploaded_cv_banner_appears_once_on_page_one()
    {
        var service = Service();
        var model = LongProfile(hasUploadedOwnCv: true);
        var pages = await RenderPages(service, model);
        Assert.True(pages.Count >= 2, $"Expected a multi-page CV, got {pages.Count}.");

        var hits = pages.Select(p => Count(p, "Eigen CV toegevoegd")).ToList();
        Assert.Equal(1, hits.Sum());
        Assert.Equal(1, hits[0]);
    }

    [Fact]
    public async Task WhoAmI_story_is_not_rendered_for_live_profile_or_application_snapshot()
    {
        var service = Service();
        var who = new LobsyCvWhoAmI(
            "ZZ-AI-MARKER-WHOAMI",
            ["samenwerken"],
            [new LobsyCvScoreBar("samenwerken", 88)],
            [new LobsyCvScoreBar("mensen meenemen", 80)]);

        var plain = Live("Ada Candidate", "ada@test.local", "0612345678", includeContact: true);
        var withStory = plain with { WhoAmI = who };
        var plainPages = await RenderPages(service, plain);
        var storyPages = await RenderPages(service, withStory);
        Assert.Equal(plainPages.Count, storyPages.Count);
        AssertNoWhoAmI(storyPages);

        var application = DownloadApplication();
        application.SnapshotWhoAmIJson = """{"story":"ZZ-AI-MARKER-WHOAMI","keywords":["samenwerken"]}""";
        var fromSnapshot = LobsyCvModelFactory.FromApplicationForDownload(
            application, includePii: true, includeDirectContact: true);
        Assert.Null(fromSnapshot.WhoAmI);
        AssertNoWhoAmI(await RenderPages(service, fromSnapshot));
    }

    [Fact]
    public async Task Work_cards_and_section_titles_stay_together()
    {
        var service = Service();
        var model = LongProfile(hasUploadedOwnCv: true);
        var pages = await RenderPages(service, model);
        Assert.True(pages.Count >= 2);

        for (var i = 0; i < model.Employers.Count; i++)
        {
            var employer = model.Employers[i];
            var namePage = IndexOfPage(pages, employer.EmployerName);
            var descPage = IndexOfPage(pages, $"DESCTOKEN{i + 1}");
            Assert.True(namePage >= 0, $"Missing employer {employer.EmployerName}");
            Assert.Equal(namePage, descPage);
        }

        await AssertNoOrphanTitles(service, model);
    }

    [Theory]
    [InlineData("Marta Kowalska (fictief)", "Lobsy-CV-MKF-20261003.pdf")]
    [InlineData("  ", "Lobsy-CV-XX-20261003.pdf")]
    public void BuildFileName_uses_letter_initials_only(string fullName, string expected)
    {
        var service = Service();
        var model = Live(fullName, "ada@test.local", null, includeContact: false) with
        {
            GeneratedAtUtc = Generated
        };
        Assert.Equal(expected, service.BuildFileName(model));
    }

    private static void AssertNoDobOrAge(IReadOnlyList<string> pages, DateOnly dob, int? ageYears)
    {
        var formatted = dob.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("nl-NL"));
        var agePattern = new Regex(@"\b\d{1,3}\s*jaar\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var page in pages)
        {
            Assert.DoesNotContain("Geboortedatum", page, StringComparison.Ordinal);
            Assert.DoesNotContain("Leeftijd", page, StringComparison.Ordinal);
            Assert.DoesNotContain(formatted, page, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotMatch(agePattern, page);
        }

        if (ageYears is int age)
        {
            Assert.DoesNotContain($"{age} jaar", string.Join('\n', pages), StringComparison.Ordinal);
        }
    }

    private static void AssertNoWhoAmI(IReadOnlyList<string> pages)
    {
        var all = string.Join('\n', pages);
        Assert.DoesNotContain("ZZ-AI-MARKER-WHOAMI", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Wie ben ik", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Persoonsprofiel bijgevoegd", all, StringComparison.Ordinal);
    }

    private static bool IsSectionTitle(string line)
    {
        var compact = Regex.Replace(line, @"\s+", " ").Trim();
        return compact.Equals("Werkervaring", StringComparison.Ordinal)
               || compact.StartsWith("Certificaten", StringComparison.Ordinal) && compact.Length < 40;
    }

    private static string LastContentLine(Page page)
    {
        var lines = page.GetWords()
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom))
            .Select(g => (
                Y: g.Average(w => w.BoundingBox.Bottom),
                Text: string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))))
            .OrderBy(l => l.Y)
            .ToList();

        var index = 0;
        while (index < lines.Count && IsFooter(lines[index].Text))
        {
            index++;
        }

        return index < lines.Count ? lines[index].Text : string.Empty;
    }

    private static bool IsFooter(string text)
        => text.Contains("Gegenereerd", StringComparison.Ordinal)
           || text.Contains("consent", StringComparison.OrdinalIgnoreCase);

    private static int IndexOfPage(IReadOnlyList<string> pages, string needle)
    {
        for (var i = 0; i < pages.Count; i++)
        {
            if (pages[i].Contains(needle, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static int Count(string haystack, string needle)
    {
        var count = 0;
        var start = 0;
        while ((start = haystack.IndexOf(needle, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += needle.Length;
        }

        return count;
    }

    private static async Task<IReadOnlyList<string>> RenderPages(LobsyCvPdfService service, LobsyCvModel model)
    {
        var pdf = await service.RenderAsync(model);
        using var doc = PdfDocument.Open(pdf);
        return doc.GetPages().Select(p => p.Text).ToList();
    }

    private static async Task AssertNoOrphanTitles(LobsyCvPdfService service, LobsyCvModel model)
    {
        var pdf = await service.RenderAsync(model);
        using var doc = PdfDocument.Open(pdf);
        foreach (var page in doc.GetPages())
        {
            var last = LastContentLine(page);
            Assert.False(IsSectionTitle(last), $"Orphan section title on a page: '{last}'");
        }
    }

    private static LobsyCvModel Live(string name, string? email, string? phone, bool includeContact)
    {
        var model = LobsyCvModelFactory.FromLiveProfile(
            name,
            email,
            phone,
            whatsAppContactAllowed: true,
            preferences: new CandidatePreferencesDto(
                Roles: ["horeca"],
                MaxTravelMinutes: 30,
                PreferredTransport: "Fiets",
                AboutMe: "Ik zoek werk in de buurt.",
                DrivingLicenses: ["B"],
                Educations: ["MBO"],
                Availability: new Dictionary<string, string[]> { ["Ma"] = ["Ochtend"] },
                MinHoursPerWeek: 8,
                MaxHoursPerWeek: 16),
            latitude: null,
            longitude: null,
            generatedAtUtc: Generated,
            dateOfBirth: Birth);
        return includeContact ? model : model with { IncludeContactDetails = false, Email = null, PhoneNumber = null };
    }

    private static LobsyCvModel LongProfile(bool hasUploadedOwnCv)
    {
        var employers = Enumerable.Range(1, 6)
            .Select(i => new CandidateEmployerHistoryDto(
                $"Werkgever EMPNAME{i}",
                "Medewerker",
                2,
                $"DESCTOKEN{i} " + string.Join(' ', Enumerable.Repeat($"taakpakket nummer {i} in het team.", 18)),
                "2020-01",
                "2024-06"))
            .ToList();
        var certificates = Enumerable.Range(1, 5)
            .Select(i => new CandidateCertificateDto($"Certificaat {i} BHV", 2020 + i))
            .ToList();

        return LobsyCvModelFactory.FromLiveProfile(
            "Marta Kowalska",
            "marta@test.local",
            "0611111111",
            false,
            new CandidatePreferencesDto(
                Roles: ["logistiek"],
                MaxTravelMinutes: 40,
                PreferredTransport: "Fiets",
                AboutMe: "Over mij " + string.Join(' ', Enumerable.Repeat("ervaring in het magazijn en de bediening bij verschillende teams.", 40)),
                DefaultMotivation: "Motivatie " + string.Join(' ', Enumerable.Repeat("ik wil dicht bij huis werken in een vast ritme.", 24)),
                DrivingLicenses: ["B"],
                Educations: ["MBO"],
                Employers: employers,
                Certificates: certificates,
                Availability: new Dictionary<string, string[]>
                {
                    ["Ma"] = ["Ochtend", "Middag"],
                    ["Di"] = ["Avond"],
                    ["Wo"] = ["Ochtend"]
                },
                MinHoursPerWeek: 12,
                MaxHoursPerWeek: 24),
            null,
            null,
            Generated,
            dateOfBirth: Birth,
            hasUploadedOwnCv: hasUploadedOwnCv);
    }

    private static Application DownloadApplication()
        => new()
        {
            CandidateName = "Bert Bijbaan",
            CandidateEmail = "bert@test.local",
            SnapshotPhoneNumber = "0612345678",
            SnapshotWhatsAppAllowed = true,
            SnapshotDateOfBirth = Birth,
            CandidateAgeYears = AgeRules.AgeYearsFromDateOfBirth(Birth),
            Motivation = "Graag starten",
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 12,
            Status = ApplicationStatus.Hired,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            Vacancy = new Vacancy
            {
                Title = "Magazijnmedewerker",
                Company = new Company
                {
                    Name = "Demo BV",
                    Address = "Industrieweg 1",
                    VerificationStatus = CompanyVerificationStatus.Verified,
                    VerificationMethod = CompanyVerificationMethod.AdminCreated,
                    VerifiedAtUtc = DateTime.UtcNow,
                    VerificationUpdatedAtUtc = DateTime.UtcNow
                },
                Location = new GeoPoint(51.99, 4.21)
            }
        };

    private static LobsyCvPdfService Service()
        => new(new LayoutCompanySettings(), new LayoutMapImages());

    private sealed class LayoutCompanySettings : IPlatformCompanySettingsService
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

    private sealed class LayoutMapImages : ICandidateMapImageService
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
}
