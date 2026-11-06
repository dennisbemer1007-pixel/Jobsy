using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

/// <summary>G78 occupation suggestions with match % (≥70%, max 2 per beroepsgroep, max 5).</summary>
public static class PupilG78JobSuggestions
{
    public static IReadOnlyList<PupilG78JobSuggestionDto> Build(
        PupilResult result,
        PupilProgress? progress,
        string language = "nl")
    {
        var vak = PupilVakScores.Parse(progress?.VakScoresJson);
        var matches = PupilG78CareerScoring.TopJobMatches(result, progress, vak);
        if (matches.Count == 0)
        {
            return [];
        }

        var english = PupilVerhaalLocalized.IsEnglish(language);
        var list = new List<PupilG78JobSuggestionDto>(matches.Count);
        foreach (var match in matches)
        {
            var key = match.JobKey;
            var beroepKey = PupilG78CareerScoring.PrimaryBeroepssoortKey(key);
            var domain = beroepKey is null
                ? ""
                : PupilG78BeroepssoortCatalog.Title(beroepKey, english);
            list.Add(new PupilG78JobSuggestionDto(
                PupilPassportLocalizedCopy.JobTitle(key, language),
                domain,
                PupilG78CareerScoring.MatchPercentInt(match.MatchScore),
                PupilPassportLocalizedCopy.JobBlurb(key, PupilQuestionSet.Groep78, language)));
        }

        return list;
    }
}
