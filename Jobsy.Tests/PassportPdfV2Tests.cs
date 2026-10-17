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
        Assert.Contains("Lobsy-geverifieerd", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("4/4 DNA-tests", text, StringComparison.Ordinal);
        Assert.Contains("Samen & aardig", text, StringComparison.Ordinal);
        Assert.Contains("Ik werk graag met mijn handen", text, StringComparison.Ordinal);
        Assert.Contains("Kwekerij De Voorbeeldtuin", text, StringComparison.Ordinal);
        Assert.Contains("marta@example.com", text, StringComparison.Ordinal);
        Assert.Contains("Gespreksinput", text, StringComparison.Ordinal);
        Assert.Contains("Zoekt:", text, StringComparison.Ordinal);
        Assert.Contains("Contract:", text, StringComparison.Ordinal);
        Assert.Contains("uitzend of vast", text, StringComparison.Ordinal);
        Assert.DoesNotContain("→", text, StringComparison.Ordinal);
        Assert.Contains("Magazijnmedewerker", text, StringComparison.Ordinal);
        Assert.Contains("Heftruck", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1994", text, StringComparison.Ordinal);
        Assert.DoesNotContain("32 jaar", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Past goed", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Huisvesting", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ZZ-AI-MARKER", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Voorbeeldstraat", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Jouw DNA in één oogopslag", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\uFFFD', text);
    }

    [Fact]
    public async Task Pdf_has_no_replacement_glyphs()
    {
        var pdf = await Render(FullFacts());
        var text = TextOf(pdf);
        Assert.DoesNotContain('\uFFFD', text);
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
        Assert.Equal(1, pdf.Pages);
        Assert.Contains("Marta Kowalska", text, StringComparison.Ordinal);
        Assert.Contains("0/4 DNA-tests", text, StringComparison.Ordinal);
        Assert.Contains("Nog niets gecontroleerd", text, StringComparison.Ordinal);
        Assert.Contains("Nog aan te vullen", text, StringComparison.Ordinal);
        Assert.Contains("Vul je paspoort aan", text, StringComparison.Ordinal);
        Assert.DoesNotContain("4/4", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Sterke punten", text, StringComparison.Ordinal);
        Assert.DoesNotContain("In mijn eigen woorden", text, StringComparison.Ordinal);
        Assert.DoesNotContain("4/4 DNA-tests", text, StringComparison.Ordinal);
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
    public async Task Recruiter_vacancy_cards_omit_scores_and_use_adult_section_title()
    {
        var vacancyId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        var rawMatches = new List<CandidateMatchedVacancyDto>
        {
            new(
                vacancyId,
                "Activiteitenbegeleider in Seghwaert (Zoetermeer)",
                "Zorggroep Voorbeeld",
                null,
                null,
                0,
                "green",
                ["Op Innovatie / probleemoplossen scoor jij 75%. Dat past goed…"],
                [],
                false,
                null),
            new(
                vacancyId,
                "Activiteitenbegeleider (43)",
                "Zorggroep Voorbeeld",
                null,
                null,
                0,
                "green",
                ["Nog een matchscore 80%"],
                [],
                false,
                null)
        };
        var fits = PassportPdfMatchCardCopy.ToJobFits(rawMatches, "nl");
        var facts = FullFacts() with { StudentPassportLayout = false, StageVacancyMatches = fits };
        var pdf = await Render(facts);
        var text = TextOf(pdf);
        Assert.Contains("Banen die bij mij passen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Stages en bijbanen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DNA-paspoort · student", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("scoor", text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Activiteitenbegeleider in Seghwaert (Zoetermeer)", fits[0].Title);
        Assert.DoesNotContain("…", fits[0].Why, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rich_layout_uses_mock_sections_without_scores()
    {
        var pdf = await Render(FullFacts());
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("Sterke punten", text, StringComparison.Ordinal);
        Assert.Contains("Talen", text, StringComparison.Ordinal);
        Assert.Contains("Werkvoorkeuren", text, StringComparison.Ordinal);
        Assert.Contains("Ervaring", text, StringComparison.Ordinal);
        Assert.Contains("Sectoren die bij mij passen", text, StringComparison.Ordinal);
        Assert.Contains("Mijn DNA in 4 lagen", text, StringComparison.Ordinal);
        Assert.Contains("Hoe ik graag werk", text, StringComparison.Ordinal);
        Assert.Contains("Zo haal je het beste uit", text, StringComparison.Ordinal);
        Assert.Contains("Gespreksinput", text, StringComparison.Ordinal);
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
        Assert.DoesNotContain("Je hebt nog geen werkervaring ingevuld.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Nog geen referenties via Lobsy.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Vrijwilligerswerk", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Mantelzorg", text, StringComparison.Ordinal);
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
        Assert.Equal(2, pdf.Pages);
        Assert.DoesNotContain("%", TextOf(pdf), StringComparison.Ordinal);
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
    public async Task Export_visual_matrix_when_a_folder_is_set()
    {
        var dir = Environment.GetEnvironmentVariable("PASSPORT_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        var scenarios = new (string Slug, PassportPdfFacts Facts)[]
        {
            ("marta-nl", FullFacts() with
            {
                Language = "nl",
                QrVerifyUrl = "https://lobsy.nl/candidate/paspoort/share/demo-marta",
                QrValidUntilLabel = "Geldig tot 3 okt 2027"
            }),
            ("rich-nl", RichSample() with { Language = "nl" }),
            ("thin-nl", ThinSample()),
            ("full-en", FullFacts() with { Language = "en" }),
            ("full-pl", FullFacts() with
            {
                Language = "pl",
                FullName = "Złotysław Żółty-Ąęść",
                OwnWords = "Pracuję dokładnie i lubię pracę zespołową w szklarni."
            }),
            ("full-ro", FullFacts() with { Language = "ro" }),
            ("rich-ar", RichSample() with { Language = "ar" }),
            ("sparse-nl", FullFacts() with
            {
                OpenForWork = false,
                WorkRegion = null,
                AvailableFrom = null,
                Roles = null,
                Experience = null,
                Certificates = null,
                Educations = null,
                OwnWords = null,
                SpokenLanguages = null,
                DutchLevel = null,
                WorkPreferences = null,
                ShareEmployerPreferences = false,
                EmployerPreferences = null,
                ContractPreferences = null,
                Dna = PassportDnaLayer.None(),
                EmailVerified = false,
                DreamTitle = null,
                ReferenceQuotes = null,
                MinHours = null,
                MaxHours = null,
                FlexibleTimes = false,
                Availability = null
            }),
            ("long-nl", FullFacts() with
            {
                FullName = "Christoffel van den Berg-Hendriksma",
                OwnWords = new string('Z', 420)
            })
        };

        foreach (var (slug, facts) in scenarios)
        {
            var pdf = await Render(facts);
            var expectedPages = slug == "sparse-nl" ? 1 : 2;
            Assert.Equal(expectedPages, pdf.Pages);
            var pdfPath = Path.Combine(dir, $"{slug}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdf.Bytes);
            ExportPdfPagesAsPng(pdfPath, Path.Combine(dir, slug));
        }
    }

    private static void ExportPdfPagesAsPng(string pdfPath, string pngPrefix)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "pdftoppm",
            Arguments = $"-png -r 150 \"{pdfPath}\" \"{pngPrefix}-page\"",
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("pdftoppm not available.");
        process.WaitForExit(120_000);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
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
        var thin = await Render(ThinSample());
        var sparse = await Render(FullFacts() with
        {
            OpenForWork = false,
            WorkRegion = null,
            AvailableFrom = null,
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
            ContractPreferences = null,
            Dna = PassportDnaLayer.None(),
            EmailVerified = false,
            DreamTitle = null,
            ReferenceQuotes = null,
            MinHours = null,
            MaxHours = null,
            FlexibleTimes = false,
            Availability = null
        });
        await File.WriteAllBytesAsync(Path.Combine(dir, "rich.pdf"), rich.Bytes);
        await File.WriteAllBytesAsync(Path.Combine(dir, "thin.pdf"), thin.Bytes);
        await File.WriteAllBytesAsync(Path.Combine(dir, "sparse.pdf"), sparse.Bytes);
        Assert.Equal(2, rich.Pages);
        Assert.Equal(2, thin.Pages);
        Assert.Equal(1, sparse.Pages);
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
            Roles: ["Orderpicker", "Medewerker tuinbouw", "Productiemedewerker"],
            MaxTravelMinutes: 30,
            PreferredTransport: "Fiets",
            Language: "nl",
            AgeYears: 32,
            AboutMe: "Ik werk graag met mijn handen en houd van duidelijke afspraken. In de kas of het magazijn voel ik me thuis. Ik wil doorgroeien met vaste collega's.",
            DrivingLicenses: ["B"],
            Availability: new Dictionary<string, string[]>
            {
                ["Ma"] = ["Ochtend", "Middag"],
                ["Di"] = ["Ochtend", "Middag"],
                ["Wo"] = ["Ochtend", "Avond"]
            },
            Employers:
            [
                new CandidateEmployerHistoryDto("Kwekerij De Voorbeeldtuin", "Oogstmedewerker", 2, "Tomaten oogsten\nKwaliteit controleren en doorzetten", "2024-03", null),
                new CandidateEmployerHistoryDto("Logistiek Voorbeeld BV", "Magazijnmedewerker", 2, "Orderpicken\nHeftruck rijden binnen het magazijn", "2021-06", "2023-12")
            ],
            Educations: ["MBO 1 / Entree"],
            HomeAddress: "Voorbeeldstraat 1, 2671 AB Naaldwijk",
            MinHoursPerWeek: 32,
            MaxHoursPerWeek: 40,
            Certificates:
            [
                new CandidateCertificateDto("VCA Basis", 2024),
                new CandidateCertificateDto("Heftruck", 2023)
            ],
            SpokenLanguages:
            [
                new CandidateLanguageDto("pl", "moedertaal"),
                new CandidateLanguageDto("en", "B1")
            ],
            DutchLevel: "basis",
            EmployerPreferences: ["small-team", "learn-on-the-job", "fixed-workplace", "growth"],
            WorkPreferences: new SharedWorkPreferences("prefer", "ok-not-frost", "lifting-15", "norm-ok"),
            ShareEmployerPreferences: true,
            WorkRegion: "Westland",
            HasOwnCar: false,
            ContractPreferences: ["uitzend", "vast"],
            LearningGoals: ["Nederlands naar B1", "Reachtruck-certificaat"]);
        var done = Generated;
        IReadOnlyList<PassportDnaLayerFact> dna =
        [
            new(PassportDnaLayer.Competence, true, done.AddDays(-7), ["Samen & aardig", "Kalm blijven", "Afmaken & netjes werken"], ["Samenwerken", "Stressbestendigheid", "Resultaatgerichtheid"]),
            new(PassportDnaLayer.Career, true, done.AddDays(-6), ["Aanpakken met je handen"], ["Realistic", "Conventional"]),
            new(PassportDnaLayer.Culture, true, done.AddDays(-3), ["Samenwerken"], ["Collaboration", "PeopleFirst"]),
            new(PassportDnaLayer.Values, true, done, ["Zekerheid & traditie", "Verbinding & zorg"], ["Stability", "Connection"])
        ];
        return PassportPdfFactsFactory.FromUser(user, prefs, dna, includeContact: true, phoneVerificationRequired: false, Generated);
    }

    [Fact]
    public void Story_stays_short_and_uses_only_entered_facts()
    {
        var full = PassportPdfModelBuilder.Build(FullFacts() with { OwnWords = "Ik werk graag met mijn handen." });
        Assert.NotNull(full.Story);
        Assert.Contains("Ik werk graag met mijn handen", full.Story, StringComparison.Ordinal);
        Assert.Contains("Kwekerij De Voorbeeldtuin", full.Story, StringComparison.Ordinal);
        Assert.Contains("Uit je tests:", full.Story, StringComparison.Ordinal);
        Assert.DoesNotContain("Westland", full.Story, StringComparison.Ordinal);
        Assert.DoesNotContain("32", full.Story, StringComparison.Ordinal);
        Assert.True(full.Story!.Length <= 450);

        var thinWords = PassportPdfModelBuilder.Build(FullFacts() with
        {
            OwnWords = null,
            Experience = null,
            Dna = PassportDnaLayer.None()
        });
        Assert.Null(thinWords.Story);
    }

    [Fact]
    public void Radar_is_drawn_only_when_two_layers_are_done()
    {
        var none = PassportPdfModelBuilder.Build(FullFacts() with { Dna = PassportDnaLayer.None() });
        Assert.False(none.ShowRadar);

        var one = PassportPdfModelBuilder.Build(FullFacts() with
        {
            Dna = [new PassportDnaLayerFact(PassportDnaLayer.Competence, true, Generated, ["Samen & aardig"])]
        });
        Assert.False(one.ShowRadar);

        var full = PassportPdfModelBuilder.Build(FullFacts());
        Assert.False(full.ShowRadar);
    }

    [Fact]
    public async Task Thin_page_surfaces_real_unpaid_work_and_learning_goals()
    {
        var pdf = await Render(ThinSample());
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("Vrijwillige zorghulp", text, StringComparison.Ordinal);
        Assert.Contains("Mantelzorg", text, StringComparison.Ordinal);
        Assert.Contains("Wat ik wil leren", text, StringComparison.Ordinal);
        Assert.Contains("Nederlands op het werk", text, StringComparison.Ordinal);
        Assert.Contains("MBO 1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Kwekerij", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Jouw DNA in één oogopslag", text, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dna_pdf_blocks_are_omitted_when_the_flag_is_off()
    {
        var model = PassportPdfModelBuilder.Build(FullFacts() with { ShowDnaBlocks = false });
        Assert.False(model.ShowDnaBlocks);
        Assert.Empty(model.DnaLayers);
        Assert.Empty(model.HowWorkLines);
        Assert.Empty(model.BestOutTips);
    }

    [Fact]
    public async Task Dna_pdf_blocks_do_not_render_when_the_flag_is_off()
    {
        var pdf = await Render(FullFacts() with { ShowDnaBlocks = false });
        var text = TextOf(pdf);
        Assert.Equal(2, pdf.Pages);
        Assert.DoesNotContain(PassportPdfStrings.T("nl", "DnaTitle"), text, StringComparison.Ordinal);
        Assert.DoesNotContain(PassportPdfStrings.T("nl", "HowIWork"), text, StringComparison.Ordinal);
        Assert.DoesNotContain("Zo haal je het beste uit", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Learning_goals_flow_from_preferences()
    {
        var user = new User
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeee01"),
            FullName = "Marta Kowalska",
            Email = "marta@example.com"
        };
        var prefs = new CandidatePreferencesDto(
            ["Orderpicker"],
            null,
            null,
            LearningGoals: ["Heftruck rijden", "Score 80%"]);
        var facts = PassportPdfFactsFactory.FromUser(
            user,
            prefs,
            PassportDnaLayer.None(),
            includeContact: false,
            phoneVerificationRequired: false,
            Generated);
        var model = PassportPdfModelBuilder.Build(facts);
        Assert.Equal(["Heftruck rijden"], model.LearningLines);
    }

    private static PassportPdfFacts ThinSample()
        => FullFacts() with
        {
            Roles = null,
            Experience =
            [
                new PassportExperienceFact("Zorghuis Wateringen", "Vrijwillige zorghulp", "2025-01", null, null, "Bewoners begeleiden"),
                new PassportExperienceFact("Oma thuis", "Mantelzorg", "2019-01", "2021-02", null, "Boodschappen en afspraken")
            ],
            Certificates = null,
            OwnWords = "Ik help graag mensen.",
            Dna = [new PassportDnaLayerFact(PassportDnaLayer.Competence, true, Generated, ["Samen & aardig"])],
            EmailVerified = false,
            DreamTitle = null,
            ReferenceQuotes = null,
            LearningGoals = ["Nederlands op het werk"]
        };

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
