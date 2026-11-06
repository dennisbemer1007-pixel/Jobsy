using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Maps vacancy list/detail DTOs onto shared kb fit view parts.</summary>
public static class KbFitMapping
{
    public static KbFitView? FromVacancy(VacancyListItem? item)
    {
        if (item is null)
        {
            return null;
        }

        if (string.Equals(item.FitGate, CandidateFitApply.FitGateClosed, StringComparison.OrdinalIgnoreCase))
        {
            return new KbFitView(GateOpen: false, Percent: null, Band: null);
        }

        var percent = item.FitPercent ?? item.MatchPercent;
        if (percent is int p)
        {
            var band = ParseBand(item.FitBand) ?? BandFromPercent(p);
            return new KbFitView(GateOpen: true, Percent: p, Band: band);
        }

        return null;
    }

    public static string? WhyLine(VacancyListItem? item, CultureState culture)
    {
        if (item is null)
        {
            return null;
        }

        if (item.FitWhyKinds is { Count: > 0 } kinds)
        {
            return CandidateFitDisplay.FormatWhyLine(kinds, key => culture[key]);
        }

        return string.IsNullOrWhiteSpace(item.FitWhyLine) ? null : item.FitWhyLine;
    }

    public static string? RankLowerLabel(VacancyListItem? item, CultureState culture)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.RankLowerReason))
        {
            return null;
        }

        var reason = culture[item.RankLowerReason];
        if (string.IsNullOrWhiteSpace(reason) || reason == item.RankLowerReason)
        {
            // Fall back to the code suffix (e.g. night-shifts → night-shifts).
            var slash = item.RankLowerReason.LastIndexOf('.');
            reason = slash >= 0 ? item.RankLowerReason[(slash + 1)..] : item.RankLowerReason;
        }

        return string.Format(culture["Kb.Rank.Lower"], reason);
    }

    public static bool GateOpen(VacancyListItem? item)
        => item is not null
           && !string.Equals(item.FitGate, CandidateFitApply.FitGateClosed, StringComparison.OrdinalIgnoreCase)
           && (item.FitPercent is not null || item.MatchPercent is not null);

    private static KbFitBand BandFromPercent(int percent)
        => CandidateFitDisplay.BandFor(percent);

    private static KbFitBand? ParseBand(string? band)
    {
        if (string.IsNullOrWhiteSpace(band))
        {
            return null;
        }

        return Enum.TryParse<KbFitBand>(band, ignoreCase: true, out var parsed) ? parsed : null;
    }
}
