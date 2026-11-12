using System.Text.RegularExpressions;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78FactStoryTests
{
    [Fact]
    public void Story_has_no_section_headings_and_uses_paragraphs()
    {
        var (result, progress) = FixtureHelpsOthers();
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        var paragraphs = PupilG78FactStory.SplitParagraphs(pkg.Body);
        Assert.InRange(paragraphs.Count, 3, 4);
        Assert.DoesNotContain("Zo kom jij over", pkg.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Ook je ", pkg.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_uses_proper_je_grammar()
    {
        var fixtures = new[] { FixtureHelpsOthers(), FixtureLeadsGroup(), FixtureMaker() };
        foreach (var (result, progress) in fixtures)
        {
            var body = PupilG78FactStory.Build(result, progress, "nl").Body;
            Assert.DoesNotContain("Ook je ", body, StringComparison.Ordinal);
            Assert.False(Regex.IsMatch(body, @"\bme\b", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bmij\b", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bmijn\b", RegexOptions.IgnoreCase));
            Assert.DoesNotContain("om me heen", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("past je vindt", body, StringComparison.OrdinalIgnoreCase);
            Assert.False(Regex.IsMatch(body, @"\b(groep|bent),?\s+helpt\s+graag", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bdat\s+(wilt|vindt|helpt)\s", RegexOptions.IgnoreCase));
            Assert.False(PupilG78QuestionJeForms.HasBareStemAfterJe(body));
        }
    }

    [Fact]
    public void Story_item_ids_are_unique_in_narrative()
    {
        var (result, progress) = FixtureHelpsOthers();
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        Assert.Equal(pkg.UsedItemIds.Count, pkg.UsedItemIds.Distinct().Count());
    }

    [Fact]
    public void Opposite_answers_produce_different_stories()
    {
        var a = PupilG78FactStory.Build(FixtureHelpsOthers().Result, FixtureHelpsOthers().Progress, "nl").Body;
        var b = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl").Body;
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void School_subjects_in_story_match_fixture_vakken()
    {
        var (result, progress) = FixtureMaker();
        var body = PupilG78FactStory.Build(result, progress, "nl").Body;
        Assert.Contains("Natuur", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Engels en Gym", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Preferences_group_school_and_hobbies()
    {
        var (result, progress) = FixtureHelpsOthers();
        var prefs = PupilG78FactStory.Build(result, progress, "nl").Preferences;
        Assert.Contains(prefs.SchoolLeuk, s => s.Contains("Teken", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prefs.HobbyLeuk, s => s.Contains("Sport", StringComparison.OrdinalIgnoreCase));
        Assert.InRange(prefs.BelangrijkItems.Count, 2, 4);
    }

    [Fact]
    public void Every_g78_item_je_form_has_no_bare_stem_after_je()
    {
        for (var id = 9001; id <= 9060; id++)
        {
            var je = PupilG78QuestionJeForms.ForItem(id);
            Assert.NotNull(je);
            Assert.False(PupilG78QuestionJeForms.HasBareStemAfterJe(je!), $"item {id}: {je}");
        }
    }

    [Fact]
    public void Story_has_no_invented_filler_or_stiff_openers()
    {
        var fixtures = new[] { FixtureHelpsOthers(), FixtureLeadsGroup(), FixtureMaker() };
        foreach (var (result, progress) in fixtures)
        {
            var body = PupilG78FactStory.Build(result, progress, "nl").Body;
            Assert.DoesNotContain("Daardoor weet je steeds beter", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Voor school en later geldt", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Al met al sluit dit aan", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Je bent dus iemand", body, StringComparison.Ordinal);
            Assert.DoesNotContain("geboren leider", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("past perfect", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("antwoorden", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Story_surfaces_competency_dimensions()
    {
        foreach (var (result, progress) in new[] { FixtureHelpsOthers(), FixtureLeadsGroup(), FixtureMaker() })
        {
            var pkg = PupilG78FactStory.Build(result, progress, "nl");
            Assert.True(
                pkg.Body.Contains("samenwerken", StringComparison.OrdinalIgnoreCase)
                || pkg.Body.Contains("creativiteit", StringComparison.OrdinalIgnoreCase)
                || pkg.Body.Contains("plannen", StringComparison.OrdinalIgnoreCase)
                || pkg.Body.Contains("communiceren", StringComparison.OrdinalIgnoreCase)
                || pkg.Body.Contains("doorzetten", StringComparison.OrdinalIgnoreCase),
                pkg.Body);
            Assert.InRange(pkg.GrowthCompetencies.Count, 1, 5);
            Assert.Contains("minder naar voren", pkg.Body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Story_and_tiles_have_no_trait_contradiction()
    {
        foreach (var (result, progress) in new[]
                 {
                     FixtureHelpsOthers(), FixtureLeadsGroup(), FixtureMaker()
                 })
        {
            var pkg = PupilG78FactStory.Build(result, progress, "nl");
            var all = new List<string> { pkg.Body };
            all.AddRange(pkg.Tiles.Select(t => t.Explanation));
            Assert.False(PupilG78AnswerConflicts.TextsContradict(all));
        }
    }

    [Fact]
    public void Story_does_not_copy_tile_sentences_verbatim()
    {
        var pkg = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl");
        foreach (var tile in pkg.Tiles)
        {
            foreach (var sentence in tile.Explanation.Split(". ", StringSplitOptions.TrimEntries))
            {
                var s = sentence.TrimEnd('.');
                if (s.Length > 35)
                {
                    Assert.DoesNotContain(s + ".", pkg.Body, StringComparison.Ordinal);
                    Assert.DoesNotContain(s + ",", pkg.Body, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Renderer_g78_has_four_substantive_tiles()
    {
        var renderer = new PupilStoryRenderer();
        var (result, progress) = FixtureLeadsGroup();
        var view = renderer.Render(result, progress, "nl");
        Assert.Equal(4, view.Tiles.Count);
        foreach (var tile in view.Tiles)
        {
            Assert.InRange(tile.Explanation.Length, 40, 2000);
            Assert.InRange(CountSentences(tile.Explanation), 2, 6);
            Assert.DoesNotContain("Je gaf zelf antwoord op deze vragen", tile.Explanation, StringComparison.Ordinal);
        }

        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        Assert.InRange(PupilG78FactStory.SplitParagraphs(pkg.Body).Count, 3, 4);
    }

    [Fact]
    public void Tile_item_ids_do_not_overlap()
    {
        var pkg = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl");
        Assert.Equal(pkg.UsedItemIds.Count, pkg.UsedItemIds.Distinct().Count());
        Assert.InRange(pkg.UsedItemIds.Count, 6, 32);
    }

    [Fact]
    public void Write_full_result_pages_for_pr()
    {
        Directory.CreateDirectory("/opt/cursor/artifacts");
        var profiles = new[]
        {
            ("Helpt anderen", FixtureHelpsOthers()),
            ("Neemt voortouw", FixtureLeadsGroup()),
            ("Maker", FixtureMaker()),
        };
        var renderer = new PupilStoryRenderer();
        var report = new System.Text.StringBuilder();
        foreach (var (name, fixture) in profiles)
        {
            var pkg = PupilG78FactStory.Build(fixture.Result, fixture.Progress, "nl");
            var view = renderer.Render(fixture.Result, fixture.Progress, "nl");
            report.AppendLine($"# {name}");
            report.AppendLine();
            report.AppendLine("## Story");
            report.AppendLine(pkg.Body);
            report.AppendLine();
            foreach (var tile in view.Tiles)
            {
                report.AppendLine($"## {tile.KidLabel}");
                report.AppendLine(tile.Explanation);
                report.AppendLine();
            }

            if (view.GrowthCompetencies is { Count: > 0 })
            {
                report.AppendLine("## Hier kun je nog in groeien");
                foreach (var g in view.GrowthCompetencies)
                {
                    report.AppendLine($"- **{g.Label}**: {g.WhyExplanation}");
                }

                report.AppendLine();
            }

            report.AppendLine("---");
            report.AppendLine();
        }

        var reportText = report.ToString();
        if (reportText.Length > 512_000)
        {
            reportText = reportText[..512_000];
        }

        File.WriteAllText("/opt/cursor/artifacts/g78-dit-ben-jij-full-result-pages.md", reportText);
        _ = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl");
    }

    private static int CountSentences(string text)
        => text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static (PupilResult Result, PupilProgress Progress) FixtureHelpsOthers()
        => PupilG78RealisticFixtures.HelpsOthers();

    private static (PupilResult Result, PupilProgress Progress) FixtureLeadsGroup()
        => PupilG78RealisticFixtures.LeadsGroup();

    private static (PupilResult Result, PupilProgress Progress) FixtureMaker()
        => PupilG78RealisticFixtures.Maker();
}
