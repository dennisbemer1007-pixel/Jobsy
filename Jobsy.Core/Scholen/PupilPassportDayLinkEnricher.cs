using Jobsy.Core.Careers;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Core.Scholen;

public static class PupilPassportDayLinkEnricher
{
    public static async Task<PupilG78PassportWebDto> WithDayLinksAsync(
        PupilG78PassportWebDto dto,
        IReadOnlyList<string>? topJobKeys,
        IOccupationDayInLifeReader reader,
        bool occupationDayEnabled,
        CancellationToken cancellationToken)
    {
        if (!dto.Enabled
            || !occupationDayEnabled
            || dto.OccupationCards is not { Count: > 0 } cards
            || topJobKeys is not { Count: > 0 })
        {
            return dto;
        }

        var escoPerCard = new List<string?>(cards.Count);
        for (var i = 0; i < cards.Count; i++)
        {
            var key = topJobKeys.ElementAtOrDefault(i);
            escoPerCard.Add(OccupationDayEscoResolve.FromJobKey(key));
        }

        var stored = await OccupationDayPassportLinks.FilterStoredAsync(reader, escoPerCard, cancellationToken);
        var enriched = cards
            .Select((card, i) =>
            {
                var escoId = escoPerCard[i];
                var dayId = escoId is not null && stored.Contains(escoId) ? escoId : null;
                return card with { DayEscoId = dayId };
            })
            .ToList();

        return dto with { OccupationCards = enriched };
    }
}
