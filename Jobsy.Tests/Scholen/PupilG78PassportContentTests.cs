using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilG78PassportContentTests
{
    [Fact]
    public void Mirror_template_resolves_chip_keys_not_raw_resource_ids()
    {
        var result = new PupilResult { HollandCode = "IS", TopValue = "Connection" };
        var progress = new PupilProgress { LikesJson = """["sport","dieren"]""" };
        var text = PupilG78MirrorTemplate.Render(result, progress, [], "nl");
        Assert.DoesNotContain("Leerling.Chip.", text, StringComparison.Ordinal);
        Assert.Contains("Sport", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Mirror_template_is_four_to_six_je_form_sentences()
    {
        var result = new PupilResult
        {
            HollandCode = "RI",
            TopValue = "Connection",
            DreamJobKey = "kok"
        };
        var progress = new PupilProgress
        {
            LikesJson = """["sport","natuur","techniek"]""",
            DreamJobKey = "kok",
            VakScoresJson = """{"natuur-techniek":{"leuk":5,"goed":5},"rekenen":{"leuk":5,"goed":5},"gym":{"leuk":5,"goed":5}}"""
        };
        var vak = PupilVakScores.Parse(progress.VakScoresJson);
        var text = PupilG78MirrorTemplate.Render(result, progress, vak, "nl");
        var count = PupilG78MirrorTemplate.CountSentences(text);
        Assert.InRange(count, PupilG78MirrorTemplate.MinSentences, PupilG78MirrorTemplate.MaxSentences);
        Assert.Contains(" je ", " " + text + " ", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deze leerling", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Subject_trait_lines_use_je_not_deze_leerling()
    {
        var scores = new List<PupilVakScoreEntry>
        {
            new("engels", 5, 5),
            new("geschiedenis", 4, 2)
        };
        var groups = PupilG78SubjectGroups.Build(scores, new PupilResult { TopValue = "Connection" }, "nl");
        var all = string.Join(' ', groups.SelectMany(g => g.TraitLines).Concat(groups.SelectMany(g => g.ConversationQuestions)));
        Assert.DoesNotContain("deze leerling", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("leuk bent", all, StringComparison.OrdinalIgnoreCase);
    }
}
