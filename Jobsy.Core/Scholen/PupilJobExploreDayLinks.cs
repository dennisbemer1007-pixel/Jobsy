using Jobsy.Core.Careers;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

public static class PupilJobExploreDayLinks
{
    public static async Task<IReadOnlyList<PupilJobExploreChipDto>> BuildChipsAsync(
        PupilResultPageDto result,
        IReadOnlyList<string> jobIdeaKeys,
        IOccupationDayInLifeReader reader,
        bool occupationDayEnabled,
        CancellationToken cancellationToken)
    {
        var chips = new List<(string Label, string? EscoId)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in jobIdeaKeys)
        {
            var label = PupilPassportLocalizedCopy.JobTitle(key, "nl");
            if (seen.Add(label))
            {
                chips.Add((label, OccupationDayEscoResolve.FromJobKey(key)));
            }
        }

        foreach (var suggestion in result.JobSuggestions ?? [])
        {
            if (chips.Count >= 5)
            {
                break;
            }

            if (seen.Add(suggestion.Title))
            {
                chips.Add((suggestion.Title, OccupationDayEscoResolve.FromTitle(suggestion.Title)));
            }
        }

        if (!occupationDayEnabled || chips.Count == 0)
        {
            return chips.Select(c => new PupilJobExploreChipDto(c.Label, null)).ToList();
        }

        var stored = await OccupationDayPassportLinks.FilterStoredAsync(
            reader,
            chips.Select(c => c.EscoId),
            cancellationToken);
        return chips
            .Select(c => new PupilJobExploreChipDto(
                c.Label,
                c.EscoId is not null && stored.Contains(c.EscoId) ? c.EscoId : null))
            .ToList();
    }
}
