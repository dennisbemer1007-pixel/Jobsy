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
        var talkKeys = result is null ? [] : PupilStoryTemplates.ConversationStarterKeys(result);
        var talk = talkKeys
            .Select(k => PupilPassportLocalizedCopy.Get(k, language))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(3)
            .ToList();

        var groups = new List<PupilPassportSubjectGroup>(3);
        if (strong.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Sterk in school", "Strong at school"),
                Intro(english,
                    "Deze vakken vind je leuk én gaat het goed. Dat is een mooie combinatie.",
                    "You enjoy these subjects and you do well. That is a strong mix."),
                strong,
                TraitLines(english, valueLabel, "leuk én goed", "enjoy and do well"),
                talk));
        }

        if (growth.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Leuk om verder in te groeien", "Fun to grow in"),
                Intro(english,
                    "Je vindt het leuk, maar het kan nog beter. Oefenen helpt.",
                    "You like it, and you can still improve. Practice helps."),
                growth,
                TraitLines(english, valueLabel, "leuk", "enjoy"),
                talk.Count >= 2 ? talk.Skip(1).ToList() : talk));
        }

        if (skill.Count > 0)
        {
            groups.Add(new PupilPassportSubjectGroup(
                Title(english, "Goed, maar minder leuk", "Good, but less fun"),
                Intro(english,
                    "Je kunt het al, maar het kost misschien meer moeite. Kies later bewust.",
                    "You can do it, but it may cost more effort. Choose consciously later."),
                skill,
                TraitLines(english, valueLabel, "goed", "do well"),
                talk.Count >= 1 ? [talk[0]] : talk));
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

    private static IReadOnlyList<string> TraitLines(bool english, string? valueLabel, string nlBand, string enBand)
    {
        var band = english ? enBand : nlBand;
        var lines = new List<string>
        {
            english
                ? $"You said you {band} in these subjects."
                : $"Je gaf aan dat je hier {nlBand} bent."
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
