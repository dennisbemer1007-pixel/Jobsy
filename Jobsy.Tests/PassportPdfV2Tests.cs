using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services.Passport;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

public class PassportPdfV2Tests
{
    private static readonly DateTime Generated = new(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Strings_exist_in_all_five_languages_without_percentages()
    {
        foreach (var key in PassportPdfStrings.Keys)
        {
            foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var text = PassportPdfStrings.T(lang, key);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("%", text);
            }
        }
    }

    [Fact]
    public void Shifts_follow_the_day_part_matrix()
    {
        var slots = new Dictionary<string, string[]>
        {
            ["Ma"] = ["Ochtend", "Middag"],
            ["Di"] = ["Ochtend"],
            ["Wo"] = ["Avond"]
        };

        var shifts = PassportPdfModelBuilder.DeriveShifts(slots, flexibleTimes: false);
        Assert.Equal(PassportShiftKind.Yes, shifts.Single(s => s.Code == "Ochtend").Kind);
        Assert.Equal(PassportShiftKind.Consult, shifts.Single(s => s.Code == "Middag").Kind);
        Assert.Equal(PassportShiftKind.Consult, shifts.Single(s => s.Code == "Avond").Kind);
        Assert.Equal(PassportShiftKind.No, shifts.Single(s => s.Code == "Nacht").Kind);
    }

    [Fact]
    public void Dna_words_are_labels_not_percents()
    {
        var words = PassportDnaWords.Competency(
            new CompetencyScores(Samenwerken: 73, Resultaatgerichtheid: 10, Stressbestendigheid: 11, Innovatie: 12),
            "nl");
        Assert.Contains("Samen & aardig", words);
        Assert.DoesNotContain(words, word => word.Contains('%') || word.Contains('7'));
    }

    [Fact]
    public async Task Full_profile_is_two_pages_of_words_without_scores_or_birth_date()
    {
        var pdf = await Render(FullFacts());
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("Marta Kowalska", text, StringComparison.Ordinal);
        Assert.Contains("Westland", text, StringComparison.Ordinal);
        Assert.Contains("DNA-paspoort", text, StringComparison.Ordinal);
        Assert.Contains("Lobsy-compleet", text, StringComparison.Ordinal);
        Assert.Contains("Samen & aardig", text, StringComparison.Ordinal);
        Assert.Contains("Ik werk graag met mijn handen.", text, StringComparison.Ordinal);
        Assert.Contains("Kwekerij De Voorbeeldtuin", text, StringComparison.Ordinal);
        Assert.Contains("marta@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1994", text, StringComparison.Ordinal);
        Assert.DoesNotContain("32 jaar", text, StringComparison.Ordinal);
        Assert.DoesNotContain("geverifieerd", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Past goed", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Huisvesting", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ZZ-AI-MARKER", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Voorbeeldstraat", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Empty_profile_omits_empty_blocks_and_stays_on_two_pages()
    {
        var facts = FullFacts() with
        {
            OpenForWork = false,
            WorkRegion = null,
            AvailableFrom = null,
            MinHours = null,
            MaxHours = null,
            FlexibleTimes = false,
            Availability = null,
            PreferredTransport = null,
            Licenses = null,
            MaxTravelMinutes = null,
            HasOwnCar = null,
            Email = null,
            Phone = null,
            WhatsApp = false,
            SpokenLanguages = null,
            DutchLevel = null,
            WorkPreferences = null,
            ShareEmployerPreferences = false,
            EmployerPreferences = null,
            Roles = null,
            ContractPreferences = null,
            Experience = null,
            Certificates = null,
            Educations = null,
            OwnWords = null,
            Motivation = null,
            Dna = PassportDnaLayer.None(),
            EmailVerified = false,
            PhoneVerified = false
        };

        var pdf = await Render(facts);
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("Marta Kowalska", text, StringComparison.Ordinal);
        Assert.Contains("Nog niet gedaan", text, StringComparison.Ordinal);
        Assert.Contains("0/4 DNA-tests afgerond", text, StringComparison.Ordinal);
        Assert.DoesNotContain("4/4", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Werkvoorkeuren", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Sterke punten", text, StringComparison.Ordinal);
        Assert.DoesNotContain("In mijn eigen woorden", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task English_pdf_uses_english_labels()
    {
        var pdf = await Render(FullFacts() with { Language = "en" });
        var text = TextOf(pdf);
        Assert.Contains("DNA passport", text, StringComparison.Ordinal);
        Assert.Contains("Languages", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Talen", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Contact_stays_off_when_it_is_not_released()
    {
        var pdf = await Render(FullFacts() with { IncludeContact = false, Email = null, Phone = null, WhatsApp = false });
        var text = TextOf(pdf);
        Assert.DoesNotContain("marta@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain("0000", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rich_layout_uses_mock_sections_without_scores()
    {
        var pdf = await Render(FullFacts());
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("Mijn verhaal", text, StringComparison.Ordinal);
        Assert.Contains("Jouw sterkste eigenschappen", text, StringComparison.Ordinal);
        Assert.Contains("Hier voel je je thuis", text, StringComparison.Ordinal);
        Assert.Contains("Wat je belangrijk vindt", text, StringComparison.Ordinal);
        Assert.Contains("Jouw DNA in één oogopslag", text, StringComparison.Ordinal);
        Assert.Contains("Werk dat bij je past", text, StringComparison.Ordinal);
        Assert.Contains("Praktisch", text, StringComparison.Ordinal);
        Assert.Contains("Werkervaring", text, StringComparison.Ordinal);
        Assert.Contains("Deel alleen als jij dat wilt", text, StringComparison.Ordinal);
        Assert.Contains("denkt mee in het team", text, StringComparison.Ordinal);
        Assert.Contains("Orderpicker", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Samira", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sparse_profile_does_not_invent_experience_quotes_or_jobs()
    {
        var facts = FullFacts() with
        {
            OpenForWork = false,
            WorkRegion = null,
            AvailableFrom = null,
            MinHours = null,
            MaxHours = null,
            FlexibleTimes = false,
            Availability = null,
            PreferredTransport = null,
            Licenses = null,
            MaxTravelMinutes = null,
            HasOwnCar = null,
            Email = null,
            Phone = null,
            WhatsApp = false,
            SpokenLanguages = null,
            DutchLevel = null,
            WorkPreferences = null,
            ShareEmployerPreferences = false,
            EmployerPreferences = null,
            Roles = null,
            ContractPreferences = null,
            Experience = null,
            Certificates = null,
            Educations = null,
            OwnWords = null,
            Motivation = null,
            Dna = PassportDnaLayer.None(),
            EmailVerified = false,
            PhoneVerified = false,
            DreamTitle = null,
            ReferenceQuotes = null
        };

        var pdf = await Render(facts);
        var text = TextOf(pdf);
        Assert.Contains("Je hebt nog geen werkervaring ingevuld.", text, StringComparison.Ordinal);
        Assert.Contains("Nog geen gedeelde referentie.", text, StringComparison.Ordinal);
        Assert.Contains("Nog niet gedaan", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Kwekerij", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Orderpicker", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ze pakt het werk rustig op.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Samira", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reference_quote_is_shown_only_when_a_real_answer_is_supplied()
    {
        var without = PassportPdfModelBuilder.Build(FullFacts());
        Assert.Empty(without.Quotes);

        var withQuote = PassportPdfModelBuilder.Build(FullFacts() with
        {
            ReferenceQuotes =
            [
                new PassportReferenceQuote("Jan · Kwekerij De Voorbeeldtuin", "Ze pakt het werk rustig op.")
            ]
        });
        Assert.Equal("Ze pakt het werk rustig op.", withQuote.Quotes[0].Quote);

        var dropped = PassportPdfModelBuilder.Build(FullFacts() with
        {
            ReferenceQuotes = [new PassportReferenceQuote("Jan", "Score 80%")]
        });
        Assert.Empty(dropped.Quotes);

        var pdf = await Render(FullFacts() with
        {
            ReferenceQuotes =
            [
                new PassportReferenceQuote("Jan · Kwekerij De Voorbeeldtuin", "Ze pakt het werk rustig op.")
            ],
            DreamTitle = "Teamleider logistiek"
        });
        var text = TextOf(pdf);
        Assert.Contains("Ze pakt het werk rustig op.", text, StringComparison.Ordinal);
        Assert.Contains("Teamleider logistiek", text, StringComparison.Ordinal);
        Assert.Contains("later", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Trait_glosses_exist_in_all_five_languages_without_percentages()
    {
        foreach (var key in PassportTraitCopy.Keys)
        {
            var parts = key.Split('.', 2);
            foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var text = PassportTraitCopy.Gloss(parts[0], parts[1], lang);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("%", text);
            }
        }
    }

    [Fact]
    public async Task Write_review_samples_when_a_folder_is_set()
    {
        var dir = Environment.GetEnvironmentVariable("PASSPORT_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        var rich = await Render(RichSample());
        var sparse = await Render(FullFacts() with
        {
            OpenForWork = false,
            Roles = null,
            Experience = null,
            Certificates = null,
            Educations = null,
            OwnWords = null,
            DutchLevel = null,
            SpokenLanguages = null,
            WorkPreferences = null,
            ShareEmployerPreferences = false,
            EmployerPreferences = null,
            Dna = PassportDnaLayer.None(),
            EmailVerified = false,
            DreamTitle = null,
            ReferenceQuotes = null,
            MinHours = null,
            MaxHours = null
        });
        await File.WriteAllBytesAsync(Path.Combine(dir, "rich.pdf"), rich.Bytes);
        await File.WriteAllBytesAsync(Path.Combine(dir, "sparse.pdf"), sparse.Bytes);
        Assert.Equal(2, rich.Pages);
        Assert.Equal(2, sparse.Pages);
    }

    [Fact]
    public async Task Long_profile_still_fits_on_two_pages()
    {
        var jobs = Enumerable.Range(1, 8)
            .Select(i => new PassportExperienceFact(
                "Bedrijf " + i,
                "Rol " + i,
                "2020-01",
                null,
                2,
                new string('a', 400)))
            .ToArray();
        var papers = Enumerable.Range(1, 8).Select(i => new PassportPaperFact("Papier " + i, 2020 + i)).ToArray();
        var pdf = await Render(FullFacts() with
        {
            Experience = jobs,
            Certificates = papers,
            OwnWords = new string('b', 2000)
        });
        Assert.Equal(2, pdf.Pages);
    }

    [Fact]
    public void Application_without_a_user_does_not_copy_age_or_birth_date()
    {
        var application = new Application
        {
            Id = Guid.NewGuid(),
            CandidateName = "Marta Kowalska",
            CandidateEmail = "marta@example.com",
            CandidateAgeYears = 32,
            SnapshotDateOfBirth = new DateOnly(1994, 5, 12),
            SnapshotAboutMe = "Ik pluk tomaten.",
            CandidateEmployerCount = 2,
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 30
        };

        var facts = PassportPdfFactsFactory.FromApplication(
            application,
            user: null,
            preferences: null,
            PassportDnaLayer.None(),
            includeDirectContact: false,
            phoneVerificationRequired: false,
            Generated);

        Assert.Null(facts.Email);
        Assert.Equal(2, facts.ExperienceCountWithoutNames);
        Assert.Equal("Ik pluk tomaten.", facts.OwnWords);
        var model = PassportPdfModelBuilder.Build(facts);
        Assert.DoesNotContain("32", model.FullName, StringComparison.Ordinal);
        Assert.DoesNotContain("1994", model.OwnWords ?? "", StringComparison.Ordinal);
        Assert.Contains("2 eerdere werkgevers", model.Experience[0].Title, StringComparison.Ordinal);
    }

    [Fact]
    public void Passport_sources_do_not_read_birth_date_or_the_ai_story()
    {
        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     "Jobsy.Core/Passport/PassportPdfFacts.cs",
                     "Jobsy.Core/Passport/PassportPdfModelBuilder.cs",
                     "Jobsy.Api/Passport/PassportPdfDownload.cs",
                     "Jobsy.Infrastructure/Services/Passport/PassportPdfService.cs",
                     "Jobsy.Core/Passport/PassportTraitCopy.cs"
                 })
        {
            var source = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("DateOfBirth", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CandidateAgeYears", source, StringComparison.Ordinal);
            Assert.DoesNotContain("WhoAmI", source, StringComparison.Ordinal);
            Assert.DoesNotContain("HomeAddress", source, StringComparison.Ordinal);
        }
    }

    private static async Task<(byte[] Bytes, int Pages)> Render(PassportPdfFacts facts)
    {
        var model = PassportPdfModelBuilder.Build(facts);
        var service = new PassportPdfService(new FakeCompanySettings());
        var bytes = await service.RenderAsync(model);
        using var doc = PdfDocument.Open(bytes);
        return (bytes, doc.NumberOfPages);
    }

    private static string TextOf((byte[] Bytes, int Pages) pdf)
    {
        using var doc = PdfDocument.Open(pdf.Bytes);
        return string.Join("\n", doc.GetPages().Select(page => page.Text));
    }

    private static PassportPdfFacts FullFacts()
    {
        var user = new User
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeee01"),
            FullName = "Marta Kowalska",
            Email = "marta@example.com",
            PhoneNumber = "+31600000000",
            WhatsAppContactAllowed = true,
            OpenForWork = true,
            EmailVerifiedAtUtc = Generated,
            DateOfBirth = new DateOnly(1994, 5, 12)
        };
        var prefs = new CandidatePreferencesDto(
            Roles: ["Orderpicker"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets",
            Language: "nl",
            AgeYears: 32,
            AboutMe: "Ik werk graag met mijn handen.",
            DrivingLicenses: ["B"],
            Availability: new Dictionary<string, string[]>
            {
                ["Ma"] = ["Ochtend", "Middag"],
                ["Di"] = ["Ochtend", "Middag"],
                ["Wo"] = ["Ochtend"]
            },
            Employers:
            [
                new CandidateEmployerHistoryDto("Kwekerij De Voorbeeldtuin", "Oogstmedewerker", 2, "Tomaten oogsten", "2024-03", null)
            ],
            Educations: ["MBO 1"],
            HomeAddress: "Voorbeeldstraat 1, 2671 AB Naaldwijk",
            MinHoursPerWeek: 32,
            MaxHoursPerWeek: 40,
            Certificates: [new CandidateCertificateDto("VCA Basis", 2024)],
            SpokenLanguages: [new CandidateLanguageDto("pl", "moedertaal")],
            DutchLevel: "basis",
            EmployerPreferences: ["small-team"],
            WorkPreferences: new SharedWorkPreferences("prefer", "ok-not-frost", "lifting-15", "norm-ok"),
            ShareEmployerPreferences: true,
            WorkRegion: "Westland",
            HasOwnCar: false,
            ContractPreferences: ["vast"]);
        var done = Generated;
        IReadOnlyList<PassportDnaLayerFact> dna =
        [
            new(PassportDnaLayer.Competence, true, done, ["Samen & aardig"]),
            new(PassportDnaLayer.Career, true, done, ["Aanpakken met je handen"]),
            new(PassportDnaLayer.Culture, true, done, ["Samenwerken"]),
            new(PassportDnaLayer.Values, true, done, ["Zekerheid & traditie"])
        ];
        return PassportPdfFactsFactory.FromUser(user, prefs, dna, includeContact: true, phoneVerificationRequired: false, Generated);
    }

    private static PassportPdfFacts RichSample()
    {
        var done = Generated;
        var facts = FullFacts() with
        {
            FullName = "Samira El Amrani",
            WorkRegion = "Wateringen",
            IncludeContact = false,
            Email = null,
            Phone = null,
            WhatsApp = false,
            DutchLevel = "basis",
            SpokenLanguages = [new CandidateLanguageDto("ar", null)],
            MinHours = 16,
            MaxHours = 24,
            Roles = ["Helpende zorg", "Assistent welzijn", "Gastvrouw", "Zorghulp"],
            Experience =
            [
                new PassportExperienceFact(
                    "Zorghuis Wateringen",
                    "Vrijwillige zorghulp",
                    "2025-01",
                    null,
                    null,
                    "Bewoners begeleiden bij wandelen en activiteiten\nHelpen bij maaltijden en een rustig gesprek\nAfspraken doorgeven aan het team"),
                new PassportExperienceFact(
                    "Restaurant Al Bahr, Den Haag",
                    "Bediening / gastvrouw",
                    "2021-03",
                    "2024-12",
                    null,
                    "Gasten ontvangen, bestellingen, afrekenen\nSamenwerken in een drukke avondploeg\nKlachten rustig oplossen"),
                new PassportExperienceFact(
                    "Oma (thuis, Wateringen)",
                    "Mantelzorg",
                    "2019-01",
                    "2021-02",
                    null,
                    "Dagelijkse hulp: medicijnen, boodschappen, afspraken\nContact met huisarts en familie plannen")
            ],
            Educations = ["MBO 1", "Inburgering A2"],
            Certificates =
            [
                new PassportPaperFact("Nederlands B1", null),
                new PassportPaperFact("BHV / EHBO", null),
                new PassportPaperFact("VCA Basis", 2024)
            ],
            OwnWords = "Ik werk het liefst in een warm team waar mensen elkaar helpen. Door mantelzorg leerde ik rustig blijven en doorzetten. Ik wil stappen zetten in de zorg, dichtbij huis.",
            DreamTitle = "Verzorgende IG",
            ReferenceQuotes =
            [
                new PassportReferenceQuote("Teamleider · Zorghuis Wateringen", "Samira is rustig, vriendelijk en leert snel. Ze komt afspraken na."),
                new PassportReferenceQuote("Supervisor · Restaurant Al Bahr", "Betrouwbaar in de bediening, ook op drukke avonden.")
            ],
            Dna =
            [
                new(PassportDnaLayer.Competence, true, done, ["Samen & aardig", "Kalm blijven", "Nieuwe dingen proberen", "Afmaken & netjes werken"], ["Samenwerken", "Stressbestendigheid", "Innovatie", "Resultaatgerichtheid"]),
                new(PassportDnaLayer.Career, true, done, ["Mensen helpen", "Aanpakken met je handen"], ["Social", "Realistic"]),
                new(PassportDnaLayer.Culture, true, done, ["Mensen voorop", "Samenwerken"], ["PeopleFirst", "Collaboration"]),
                new(PassportDnaLayer.Values, true, done, ["Verbinding & zorg", "Prestatie & groei", "Zekerheid & traditie", "Impact & rechtvaardigheid"], ["Connection", "Achievement", "Stability", "Impact"])
            ]
        };
        return facts;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Jobsy.sln.");
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Test", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(PlatformCompanyUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }
}
