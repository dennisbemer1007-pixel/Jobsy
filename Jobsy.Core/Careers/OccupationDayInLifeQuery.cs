using Jobsy.Core.Localization;

namespace Jobsy.Core.Careers;

public static class OccupationDayInLifeQuery
{
    public static async Task<OccupationDayPublicResponse> ToResponseAsync(
        IOccupationDayInLifeReader days,
        string escoId,
        string? lang,
        CancellationToken cancellationToken)
    {
        var language = JobsyLanguages.IsSupported(lang)
            ? JobsyLanguages.Normalize(lang)
            : JobsyLanguages.Default;
        var result = await days.GetAsync(escoId, language, cancellationToken);
        if (!result.KnownOccupation)
        {
            return Empty(enabled: result.Enabled, found: false, escoId, language);
        }

        if (!result.Enabled || result.Day is null)
        {
            var title = OccupationCatalog.Shared.Get(escoId)?.Nl;
            return Empty(result.Enabled, false, escoId, language, title);
        }

        var day = result.Day;
        return new OccupationDayPublicResponse(
            true,
            true,
            day.EscoId,
            day.TitleNl,
            day.Morning,
            day.Midday,
            day.Afternoon,
            day.Closing,
            day.Highlights,
            day.VariesNote,
            day.ThinSource,
            language,
            day.Blocks ?? [],
            day.Tasks ?? [],
            day.Skills ?? []);
    }

    private static OccupationDayPublicResponse Empty(
        bool enabled,
        bool found,
        string? escoId,
        string language,
        string? titleNl = null)
        => new(
            enabled,
            found,
            escoId,
            titleNl,
            null,
            null,
            null,
            null,
            [],
            null,
            false,
            language,
            [],
            [],
            []);
}
