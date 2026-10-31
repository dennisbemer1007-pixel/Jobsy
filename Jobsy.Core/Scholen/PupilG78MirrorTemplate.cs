using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>Deterministic mirror text for G78 passport (no invented facts).</summary>
public static class PupilG78MirrorTemplate
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static string Render(
        PupilResult result,
        PupilProgress? progress,
        IReadOnlyList<PupilVakScoreEntry> vakScores,
        string language = "nl")
    {
        var english = PupilVerhaalLocalized.IsEnglish(language);
        var likes = ParseChips(progress?.LikesJson);
        var likeLabels = (english
                ? likes.Select(PupilPortalChipLabels.English)
                : likes.Select(PupilPortalChipLabels.Dutch))
            .Take(3)
            .ToList();

        if (!string.IsNullOrWhiteSpace(progress?.LikeOtherWord))
        {
            likeLabels.Add(progress.LikeOtherWord.Trim());
        }

        var holland = (result.HollandCode ?? "S").Trim().ToUpperInvariant();
        var letter = holland.Length > 0 && "RIASEC".Contains(holland[0]) ? holland[0] : 'S';
        var riasecKid = PupilPassportLocalizedCopy.Get($"LeerlingStory.Tile.Riasec.{letter}", language);

        var strong = vakScores.Where(PupilVakScores.IsStrong).Take(3).ToList();
        var subjectBit = strong.Count == 0
            ? (english
                ? "You rated your school subjects honestly."
                : "Je gaf eerlijk antwoord over je schoolvakken.")
            : (english
                ? $"At school you feel strong in {string.Join(", ", strong.Select(s => PupilVakSubjectLabels.Nl(s.SubjectKey)))}."
                : $"Op school ben je sterk in {string.Join(", ", strong.Select(s => PupilVakSubjectLabels.Nl(s.SubjectKey)))}.");

        var likeBit = likeLabels.Count == 0
            ? ""
            : (english
                ? $" In your free time you like {string.Join(", ", likeLabels)}."
                : $" In je vrije tijd vind je {string.Join(", ", likeLabels)} leuk.");

        var valueKey = string.IsNullOrWhiteSpace(result.TopValue)
            ? null
            : $"LeerlingStory.Tile.Val.{result.TopValue.Trim()}";
        var valueBit = valueKey is null
            ? ""
            : (english
                ? $" What matters to you: {PupilPassportLocalizedCopy.Get(valueKey, language).TrimEnd('.')}."
                : $" Wat jij belangrijk vindt: {PupilPassportLocalizedCopy.Get(valueKey, language).TrimEnd('.')}.");

        return (english
                ? $"From your Lobsy journey: {riasecKid.TrimEnd('.')}."
                : $"Uit jouw Lobsy-reis: {riasecKid.TrimEnd('.')}.")
            + likeBit
            + valueBit
            + " "
            + subjectBit;
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
