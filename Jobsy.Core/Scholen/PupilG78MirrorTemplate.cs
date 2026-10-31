using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Deterministic mirror text for G78 passport (no invented facts).</summary>
public static class PupilG78MirrorTemplate
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public const int MinSentences = 4;
    public const int MaxSentences = 6;

    public static string Render(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        string language = "nl")
    {
        var english = PupilVerhaalLocalized.IsEnglish(language);
        var sentences = english
            ? BuildEnglishSentences(result, progress, vakScores)
            : BuildDutchSentences(result, progress, vakScores);

        return NormalizeSentences(sentences);
    }

    private static string NormalizeSentences(IReadOnlyList<string> sentences)
    {
        var cleaned = sentences
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(EnsurePeriod)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxSentences)
            .ToList();

        return string.Join(' ', cleaned);
    }

    private static List<string> BuildDutchSentences(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores)
    {
        var sentences = new List<string>(MaxSentences);
        var holland = (result.HollandCode ?? "IS").Trim().ToUpperInvariant();
        foreach (var letter in HollandLetters(holland).Take(2))
        {
            var desc = RiasecDesc(letter, "nl");
            if (!string.IsNullOrWhiteSpace(desc))
            {
                sentences.Add(desc);
            }
        }

        var strongFun = vakScores
            .Where(e => e.Leuk >= 4 && e.Goed >= 4)
            .OrderByDescending(e => e.Leuk + e.Goed)
            .Take(3)
            .ToList();
        if (strongFun.Count > 0)
        {
            var names = strongFun.Select(s => PupilVakSubjectLabels.Nl(s.SubjectKey)).ToList();
            sentences.Add(JoinDutchList(names) + " vind je leuk, en daar ben je ook goed in");
        }
        else
        {
            var strong = vakScores.Where(PupilVakScores.IsStrong).Take(3).ToList();
            if (strong.Count > 0)
            {
                var names = strong.Select(s => PupilVakSubjectLabels.Nl(s.SubjectKey)).ToList();
                sentences.Add("Op school ben je sterk in " + JoinDutchList(names));
            }
        }

        var likes = LikeLabels(progress, english: false);
        if (likes.Count > 0)
        {
            sentences.Add("In je vrije tijd vind je " + JoinDutchList(likes.Take(3).ToList()) + " leuk");
        }

        if (!string.IsNullOrWhiteSpace(result.TopValue))
        {
            var valueLabel = PupilPassportLocalizedCopy
                .Get($"LeerlingStory.Tile.Val.{result.TopValue.Trim()}", "nl")
                .TrimEnd('.');
            if (valueLabel.Length > 0)
            {
                sentences.Add(valueLabel + " vind je belangrijk");
            }
        }

        var dreamKey = progress?.DreamJobKey ?? result.DreamJobKey;
        if (!string.IsNullOrWhiteSpace(dreamKey))
        {
            var job = PupilPassportLocalizedCopy.JobTitle(dreamKey.Trim(), "nl");
            sentences.Add("Later wil je misschien iets doen als " + job.ToLowerInvariant());
        }

        if (sentences.Count < MinSentences)
        {
            sentences.Add("Je gaf zelf antwoord over wat bij jou past.");
        }

        if (sentences.Count < MinSentences)
        {
            sentences.Add("Herken je jezelf in wat hier staat?");
        }

        return sentences;
    }

    private static List<string> BuildEnglishSentences(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores)
    {
        var sentences = new List<string>(MaxSentences);
        var holland = (result.HollandCode ?? "IS").Trim().ToUpperInvariant();
        foreach (var letter in HollandLetters(holland).Take(2))
        {
            var desc = RiasecDesc(letter, "en");
            if (!string.IsNullOrWhiteSpace(desc))
            {
                sentences.Add(desc);
            }
        }

        var strong = vakScores.Where(PupilVakScores.IsStrong).Take(3).ToList();
        if (strong.Count > 0)
        {
            var names = strong.Select(s => PupilVakSubjectLabels.Nl(s.SubjectKey)).ToList();
            sentences.Add("At school you feel strong in " + string.Join(", ", names) + ".");
        }

        var likes = LikeLabels(progress, english: true);
        if (likes.Count > 0)
        {
            sentences.Add("In your free time you like " + string.Join(", ", likes.Take(3)) + ".");
        }

        if (!string.IsNullOrWhiteSpace(result.TopValue))
        {
            var valueLabel = PupilPassportLocalizedCopy
                .Get($"LeerlingStory.Tile.Val.{result.TopValue.Trim()}", "en")
                .TrimEnd('.');
            if (valueLabel.Length > 0)
            {
                sentences.Add("You said " + valueLabel.ToLowerInvariant() + " matters to you.");
            }
        }

        return sentences;
    }

    private static IEnumerable<char> HollandLetters(string holland)
    {
        foreach (var ch in holland)
        {
            if ("RIASEC".Contains(ch))
            {
                yield return ch;
            }
        }
    }

    private static string RiasecDesc(char letter, string language)
    {
        var key = $"LeerlingPassport.G78.Riasec.{letter}.Desc";
        var text = PupilPassportLocalizedCopy.Get(key, language).Trim();
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, key, StringComparison.Ordinal))
        {
            return "";
        }

        return EnsurePeriod(text.TrimEnd('.'));
    }

    private static List<string> LikeLabels(PupilProgress? progress, bool english)
    {
        var likes = ParseChips(progress?.LikesJson);
        var labels = (english
                ? likes.Select(PupilPortalChipLabels.English)
                : likes.Select(PupilPortalChipLabels.Dutch))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            labels.Add(progress.LikeOtherWord.Trim());
        }

        return labels;
    }

    private static string JoinDutchList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} en {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + " en " + items[^1]
    };

    private static string EnsurePeriod(string sentence)
        => sentence.EndsWith('.') ? sentence : sentence + ".";

    public static int CountSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return Regex.Matches(text, @"(?<=[.!?])\s+").Count + 1;
    }

    public static CandidateFactSheet FactSheet(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        string language)
    {
        var english = PupilVerhaalLocalized.IsEnglish(language);
        var confirmed = new List<string>();
        foreach (var chip in ParseChips(progress?.LikesJson))
        {
            var c = PupilInterestChipCatalog.LikeChips.FirstOrDefault(x =>
                string.Equals(x.Key, chip, StringComparison.OrdinalIgnoreCase));
            if (c is not null)
            {
                confirmed.Add(english
                    ? PupilPortalChipLabels.English(chip)
                    : PupilPortalChipLabels.Dutch(chip));
            }
        }

        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            confirmed.Add(progress.LikeOtherWord.Trim());
        }

        foreach (var entry in vakScores)
        {
            confirmed.Add(PupilVakSubjectLabels.Nl(entry.SubjectKey));
        }

        var holland = result.HollandCode ?? "";
        if (!string.IsNullOrWhiteSpace(holland))
        {
            confirmed.Add(holland);
        }

        if (!string.IsNullOrWhiteSpace(result.TopValue))
        {
            confirmed.Add(result.TopValue);
            confirmed.Add(PupilPassportLocalizedCopy.Get($"LeerlingStory.Tile.Val.{result.TopValue.Trim()}", language));
        }

        return CandidateFactSheet.Personal([], [], [], confirmedItems: confirmed);
    }

    private static IReadOnlyList<string> ParseChips(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
