using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

public sealed record PupilPassportSubjectLine(string SubjectKey, string Label, int Leuk, int Goed);

public sealed record PupilPassportSubjectGroup(
    string Title,
    string Intro,
    IReadOnlyList<PupilPassportSubjectLine> Subjects,
    IReadOnlyList<string> TraitLines,
    IReadOnlyList<string> ConversationQuestions);

/// <summary>Groups G78 subject ratings for passport PDF and web (3 bands).</summary>
public static class PupilG78SubjectGroups
{
    public static IReadOnlyList<PupilPassportSubjectGroup> Build(
        IReadOnlyList<PupilVakScoreEntry> scores,
        PupilResult? result,
        string language = "nl")
    {
        var english = PupilVerhaalLocalized.IsEnglish(language);
        var strong = Lines(scores, PupilVakScores.IsStrong, english);
        var growth = Lines(scores, PupilVakScores.IsLeukGrowth, english);
        var skill = Lines(scores, PupilVakScores.IsGoedOnly, english);

        var valueLabel = ValueLabel(result?.TopValue, english);
        var talk = PupilG78PassportTalk.Questions(scores, english);

        var groups = new List<PupilPassportSubjectGroup>(3);
        if (strong.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Mijn sterke vakken", "My strong subjects"),
                Intro(english,
                    "Leuk én goed in",
                    "Fun and you do well"),
                strong,
                TraitLines(english, valueLabel, strong: true),
                talk));
        }

        if (growth.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Leuk, hier groei ik in", "Fun to grow in"),
                Intro(english,
                    "Je plezier helpt je verder",
                    "Your enjoyment helps you grow"),
                growth,
                TraitLines(english, valueLabel, growth: true),
                talk));
        }

        if (skill.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Hier ben ik goed in", "Good at this"),
                Intro(english,
                    "Ook als het niet je favoriet is",
                    "Even when it is not your favourite"),
                skill,
                TraitLines(english, valueLabel, skill: true),
                talk));
        }

        return groups;
    }

    private static List<PupilPassportSubjectLine> Lines(
        IReadOnlyList<PupilVakScoreEntry> scores,
        Func<PupilVakScoreEntry, bool> pick,
        bool english)
    {
        return scores
            .Where(pick)
            .OrderBy(e => PupilVakScores.AllSubjectKeys.ToList().IndexOf(e.SubjectKey))
            .Select(e => new PupilPassportSubjectLine(
                e.SubjectKey,
                english ? PupilVakSubjectLabels.Nl(e.SubjectKey) : PupilVakSubjectLabels.Nl(e.SubjectKey),
                e.Leuk,
                e.Goed))
            .ToList();
    }

    private static string Title(bool english, string nl, string en) => english ? en : nl;

    private static string Intro(bool english, string nl, string en) => english ? en : nl;

    private static string? ValueLabel(string? topValue, bool english)
    {
        if (string.IsNullOrWhiteSpace(topValue))
        {
            return null;
        }

        var key = $"LeerlingStory.Tile.Val.{topValue.Trim()}";
        return PupilPassportLocalizedCopy.Get(key, english ? "en" : "nl");
    }

    private static IReadOnlyList<string> TraitLines(bool english, string? valueLabel, bool strong = false, bool growth = false, bool skill = false)
    {
        var lines = new List<string>
        {
            english switch
            {
                true when strong => "You said these subjects are fun and you do well in them.",
                true when growth => "You said you enjoy these subjects.",
                true when skill => "You said you do well in these subjects.",
                false when strong => "Je vindt deze vakken leuk en je bent er goed in.",
                false when growth => "Je vindt deze vakken leuk.",
                false when skill => "Je bent in deze vakken goed.",
                _ => english
                    ? "You rated these subjects yourself."
                    : "Je gaf zelf antwoord over deze vakken."
            }
        };

        if (!string.IsNullOrWhiteSpace(valueLabel))
        {
            lines.Add(english
                ? $"That fits what you find important: {valueLabel.TrimEnd('.')}."
                : $"Dat past bij wat jij belangrijk vindt: {valueLabel.TrimEnd('.')}.");
        }

        return lines;
    }
}
