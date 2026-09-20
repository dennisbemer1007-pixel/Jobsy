using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class CareerCompassTests
{
    [Fact]
    public void Bands_use_inclusive_thresholds_95_85_75()
    {
        Assert.Equal("super", CareerCompassBuilder.Band(100));
        Assert.Equal("super", CareerCompassBuilder.Band(95));
        Assert.Equal("strong", CareerCompassBuilder.Band(94));
        Assert.Equal("strong", CareerCompassBuilder.Band(85));
        Assert.Equal("broaden", CareerCompassBuilder.Band(84));
        Assert.Equal("broaden", CareerCompassBuilder.Band(75));
        Assert.Equal("", CareerCompassBuilder.Band(74));
    }

    [Fact]
    public void High_hands_on_scores_fill_super_match_occupations()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: true);
        Assert.True(compass.FromDeepAnalysis);
        Assert.Contains("Aanpakken met je handen", compass.Strengths);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("kas", StringComparison.OrdinalIgnoreCase) && m.Percent >= 95);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("bouw", StringComparison.OrdinalIgnoreCase));
        Assert.All(compass.SuperMatches, m => Assert.True(m.Percent >= 95));
        Assert.All(compass.StrongChoices, m => Assert.InRange(m.Percent, 85, 94));
        Assert.All(compass.Broadening, m => Assert.InRange(m.Percent, 75, 84));
        Assert.Contains(compass.PracticalNotes, n => n.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(compass.PracticalNotes, n => n.Contains("150 vragen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Mixed_scores_span_all_three_occupation_bands()
    {
        var compass = CareerCompassBuilder.Build(new RiasecScores(100, 40, 40, 88, 40, 78));
        Assert.True(compass.HasOccupations);
        Assert.NotEmpty(compass.SuperMatches);
        Assert.NotEmpty(compass.StrongChoices);
        Assert.NotEmpty(compass.Broadening);
        Assert.Contains(compass.SuperMatches, m => m.Title.Contains("kas", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.StrongChoices, m => m.Title.Contains("zorg", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.Broadening, m => m.Title.Contains("Administratief", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Quick_scan_practical_notes_invite_the_paid_test()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: false);
        Assert.False(compass.FromDeepAnalysis);
        Assert.Contains(
            compass.PracticalNotes,
            n => n.Contains("uitgebreide beroepentest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Incomplete_scores_yield_empty_occupations_and_a_plain_prompt()
    {
        var compass = CareerCompassBuilder.Build(null);
        Assert.False(compass.HasOccupations);
        Assert.NotEmpty(compass.PracticalNotes);
        AssertNoJargon(compass);
    }

    [Fact]
    public void Candidate_facing_copy_has_no_riasec_or_ocean_jargon()
    {
        Assert.False(CareerCompassBuilder.ContainsForbiddenJargon("sociale werkplek"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("RIASEC-profiel"));
        Assert.True(CareerCompassBuilder.ContainsForbiddenJargon("Holland-code ACE"));

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            AssertNoJargon(CareerCompassBuilder.TypeLabel(code));
        }

        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandSuper));
        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong));
        AssertNoJargon(CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandBroaden));

        foreach (var fromDeep in new[] { false, true })
        {
            foreach (var code in CareerTestCatalog.RiasecCodes)
            {
                AssertNoJargon(CareerCompassBuilder.Build(Peak(code), fromDeep));
            }
        }

        var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(
            CareerTestCatalog.RiasecCodes.Select(c => new DeepAnalysisDomainScore(c, 90, 25)).ToList());
        foreach (var line in advice)
        {
            AssertNoJargon(line);
        }
    }

    [Fact]
    public void Kompas_ui_strings_use_plain_dutch()
    {
        string[] keys =
        [
            "Kompas.Career",
            "Kompas.CareerStrengths",
            "Kompas.BandSuper",
            "Kompas.BandStrong",
            "Kompas.BandBroaden",
            "Kompas.PracticalTitle",
            "Career.Lead",
            "Career.ScienceNote",
            "Career.SavedComplete"
        ];
        foreach (var key in keys)
        {
            var nl = UiStrings.Get(key, "nl");
            Assert.False(string.IsNullOrWhiteSpace(nl));
            AssertNoJargon(nl);
        }

        Assert.Equal("Mijn Beroepen-kompas", UiStrings.Get("Kompas.Career", "nl"));
        Assert.Equal("Wat betekent dit voor jou?", UiStrings.Get("Kompas.PracticalTitle", "nl"));
        Assert.Contains("95%", UiStrings.Get("Kompas.BandSuper", "nl"));
        Assert.Contains("85%", UiStrings.Get("Kompas.BandStrong", "nl"));
        Assert.Contains("75%", UiStrings.Get("Kompas.BandBroaden", "nl"));
    }

    [Fact]
    public void Brand_logo_png_is_colored()
    {
        var png = LoadBrandLogo();
        Assert.True(png.Length > 100);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
        // IHDR color type: 2 truecolor, 3 indexed, 6 truecolor+alpha — not grayscale.
        var colorType = png[25];
        Assert.True(colorType is 2 or 3 or 6, $"Expected a color PNG, got color type {colorType}.");
    }

    [Fact]
    public void Career_pdf_uses_logo_bands_and_plain_language()
    {
        var compass = CareerCompassBuilder.Build(HandsOnScores(), fromDeepAnalysis: true);
        var withLogo = AssessmentReportPdfService.RenderCareer(
            "Lobsy", LoadBrandLogo(), "Ada Kandidaat", "20 september 2026", compass);
        var withoutLogo = AssessmentReportPdfService.RenderCareer(
            "Lobsy", [], "Ada Kandidaat", "20 september 2026", compass);

        AssertPdf(withLogo);
        AssertPdf(withoutLogo);
        Assert.True(withLogo.Length > withoutLogo.Length);

        var haystack = System.Text.Encoding.Latin1.GetString(withLogo) + PdfLiteralText(withLogo);
        Assert.Contains("loopbaanrapport", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Super-match", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sterke keus", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Handige verbreding", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Wat betekent dit voor jou?", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RIASEC", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OCEAN", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Holland-code", haystack, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Holland code", haystack, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Score_based_vacancy_fit_uses_candidate_percentages()
    {
        var scores = new RiasecScores(100, 0, 0, 10, 0, 0);
        Assert.Equal(1.0, VacancyRiasecProfile.Fit01(scores, [CareerTestCatalog.Realistic]));
        Assert.Equal(0.10, VacancyRiasecProfile.Fit01(scores, [CareerTestCatalog.Social]), 2);
    }

    [Fact]
    public void Deep_analysis_domain_scores_map_onto_riasec_percents()
    {
        var mapped = DeepAnalysisCatalog.ToRiasecScores(
        [
            new DeepAnalysisDomainScore(CareerTestCatalog.Realistic, 91, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Investigative, 40, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Artistic, 33, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Social, 22, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Enterprising, 11, 25),
            new DeepAnalysisDomainScore(CareerTestCatalog.Conventional, 5, 25)
        ]);
        Assert.True(mapped.IsComplete);
        Assert.Equal(91, mapped.Realistic);
        Assert.Equal(5, mapped.Conventional);
    }

    [Fact]
    public void Kompas_panel_and_pages_wire_career_compass()
    {
        var root = RepoRoot.Find();
        var panel = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CareerCompassPanel.razor"));
        Assert.Contains("Kompas.BandSuper", panel, StringComparison.Ordinal);
        Assert.Contains("Kompas.PracticalTitle", panel, StringComparison.Ordinal);

        var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.Career", home, StringComparison.Ordinal);
        Assert.Contains("CareerCompassPanel", home, StringComparison.Ordinal);

        var profile = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/Profile.razor"));
        Assert.Contains("CareerCompassPanel", profile, StringComparison.Ordinal);

        var merge = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/DeepAnalysisService.cs"));
        Assert.Contains("ToRiasecScores", merge, StringComparison.Ordinal);
        Assert.Contains("RealisticPercent = riasec.Realistic", merge, StringComparison.Ordinal);
    }

    private static RiasecScores HandsOnScores()
        => new(100, 20, 15, 18, 22, 25);

    private static RiasecScores Peak(string code) => code switch
    {
        CareerTestCatalog.Realistic => new(100, 20, 20, 20, 20, 20),
        CareerTestCatalog.Investigative => new(20, 100, 20, 20, 20, 20),
        CareerTestCatalog.Artistic => new(20, 20, 100, 20, 20, 20),
        CareerTestCatalog.Social => new(20, 20, 20, 100, 20, 20),
        CareerTestCatalog.Enterprising => new(20, 20, 20, 20, 100, 20),
        _ => new(20, 20, 20, 20, 20, 100)
    };

    private static void AssertNoJargon(CareerCompassSnapshot compass)
    {
        foreach (var text in compass.Strengths
                     .Concat(compass.PracticalNotes)
                     .Concat(compass.SuperMatches.SelectMany(OccupationTexts))
                     .Concat(compass.StrongChoices.SelectMany(OccupationTexts))
                     .Concat(compass.Broadening.SelectMany(OccupationTexts)))
        {
            AssertNoJargon(text);
        }
    }

    private static IEnumerable<string> OccupationTexts(CareerOccupationMatch match)
        => [match.Title, match.Why, match.Band, CareerCompassBuilder.BandLabel(match.Band)];

    private static void AssertNoJargon(string text)
        => Assert.False(
            CareerCompassBuilder.ContainsForbiddenJargon(text),
            $"Forbidden jargon in: {text}");

    private static void AssertPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    private static byte[] LoadBrandLogo()
    {
        var assembly = typeof(PlatformCompanySettingsService).Assembly;
        using var stream = assembly.GetManifestResourceStream("Jobsy.Infrastructure.Assets.lobsy.png")
            ?? throw new InvalidOperationException("Embedded Lobsy logo missing.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string PdfLiteralText(byte[] pdf)
    {
        var raw = System.Text.Encoding.Latin1.GetString(pdf);
        return string.Concat(
            System.Text.RegularExpressions.Regex.Matches(raw, @"\((?:\\.|[^\\)])*\)")
                .Select(m => m.Value.Trim('(', ')')));
    }
}
