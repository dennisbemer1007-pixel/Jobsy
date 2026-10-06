using System.Globalization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Careers;

/// <summary>
/// Up to 10 occupations that fit the candidate's own test and that employers will strongly
/// need in the Netherlands through 2030. Figures come only from the sourced outlook and the
/// sourced interest profile. Nothing is filled in when a source is missing.
/// </summary>
public static class FutureJobsForYou
{
    public const int MaxCount = 10;

    /// <summary>ROA ITKB "groot" (3) and "zeer groot" (4). Lower ranks are not "hard nodig".</summary>
    public const int StrongNeedMinRank = 3;

    public static FutureJobsList Build(RiasecScores? scores, string? education = null)
    {
        var peildatum = OccupationOutlook.Shared.Peildatum;
        if (scores is not { IsComplete: true })
        {
            return new FutureJobsList(true, [], peildatum);
        }

        var rows = new List<FutureJobRow>();
        foreach (var job in OccupationCatalog.Shared.Listable)
        {
            if (!IsEligible(job, scores, education) || job.Oi is not { Count: 6 } oi)
            {
                continue;
            }

            if (!OccupationOutlook.Shared.TryGetSourcedDemand(job.Id, out var demand) || demand is null)
            {
                continue;
            }

            rows.Add(new FutureJobRow(
                job.Id,
                DisplayTitle(job.Nl),
                CareerCompassBuilder.ProfileMatch(oi, scores),
                demand.ItkbRank,
                demand.Typering,
                demand.OpeningsPer100,
                demand.AiLine,
                WhyTraitCodes(scores, oi)));
        }

        return new FutureJobsList(false, Order(rows), peildatum);
    }

    /// <summary>
    /// A job stays out when its interest profile is not sourced, its outlook is not sourced,
    /// employers will not strongly need it, it sits above the candidate's education, or it is
    /// a leadership title while leading is not one of their top three directions.
    /// </summary>
    public static bool IsEligible(Occupation job, RiasecScores scores, string? education = null)
    {
        if (scores is not { IsComplete: true } || !job.IsListable || job.Oi is not { Count: 6 })
        {
            return false;
        }

        if (!OccupationOutlook.Shared.TryGetSourcedDemand(job.Id, out var demand)
            || demand is null
            || demand.ItkbRank < StrongNeedMinRank)
        {
            return false;
        }

        if (!CareerEducationGate.Passes(job.IscoLevel, CareerEducationGate.MaxIscoLevel(education)))
        {
            return false;
        }

        if (!CareerCompassBuilder.EnterprisingInTop3(scores) && OccupationCatalog.IsLeadership(job))
        {
            return false;
        }

        return true;
    }

    /// <summary>Best test fit first. Equal fit: stronger ROA need, then more openings per 100.</summary>
    public static IReadOnlyList<FutureJobRow> Order(IEnumerable<FutureJobRow> rows)
        => rows
            .OrderByDescending(row => row.FitPercent)
            .ThenByDescending(row => row.ItkbRank)
            .ThenByDescending(row => row.OpeningsPer100)
            .ThenBy(row => row.TitleNl, StringComparer.OrdinalIgnoreCase)
            .Take(MaxCount)
            .ToList();

    /// <summary>
    /// Up to two directions from the candidate's own top three that this job's sourced
    /// profile also asks for. When there is no overlap, the candidate's own top two.
    /// </summary>
    public static IReadOnlyList<string> WhyTraitCodes(RiasecScores scores, IReadOnlyList<double> oi)
    {
        var ranked = RiasecRanking.Rank(
            scores.Realistic,
            scores.Investigative,
            scores.Artistic,
            scores.Social,
            scores.Enterprising,
            scores.Conventional);
        var top = ranked.Take(3).Select(item => item.Code).ToList();
        var asked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var limit = Math.Min(6, oi.Count);
        for (var i = 0; i < limit; i++)
        {
            if ((decimal)oi[i] - 1m > 0m)
            {
                asked.Add(CareerTestCatalog.RiasecCodes[i]);
            }
        }

        var overlap = top.Where(asked.Contains).Take(2).ToList();
        if (overlap.Count > 0)
        {
            return overlap;
        }

        return ranked.Take(2).Select(item => item.Code).ToList();
    }

    public static string WhyNl(IReadOnlyList<string> traitCodes)
    {
        var phrases = traitCodes
            .Select(TraitPhraseNl)
            .Where(phrase => phrase.Length > 0)
            .Take(2)
            .ToList();
        return phrases.Count switch
        {
            0 => "Past bij jou: dit sluit aan bij jouw test.",
            1 => "Past bij jou: " + phrases[0] + ".",
            _ => "Past bij jou: " + phrases[0] + " en " + phrases[1] + "."
        };
    }

    public static string TraitPhraseNl(string code) => code switch
    {
        CareerTestCatalog.Realistic => "je werkt graag met je handen",
        CareerTestCatalog.Investigative => "je zoekt graag uit hoe iets werkt",
        CareerTestCatalog.Artistic => "je maakt graag iets eigens",
        CareerTestCatalog.Social => "je helpt graag andere mensen",
        CareerTestCatalog.Enterprising => "je zet graag dingen in beweging",
        CareerTestCatalog.Conventional => "je houdt van duidelijke taken",
        _ => ""
    };

    public static string FormatOpenings(double value, CultureInfo? culture = null)
    {
        var info = culture ?? CultureInfo.InvariantCulture;
        var whole = Math.Round(value, 0, MidpointRounding.AwayFromZero);
        if (Math.Abs(value - whole) < 0.001)
        {
            return whole.ToString("0", info);
        }

        return value.ToString("0.##", info);
    }

    public static string DisplayTitle(string? nl)
    {
        var trimmed = (nl ?? "").Trim();
        if (trimmed.Length == 0)
        {
            return "";
        }

        return char.ToUpper(trimmed[0], CultureInfo.GetCultureInfo("nl-NL")) + trimmed[1..];
    }
}

public sealed record FutureJobRow(
    string EscoId,
    string TitleNl,
    decimal FitPercent,
    int ItkbRank,
    string Typering,
    double OpeningsPer100,
    string? AiLine,
    IReadOnlyList<string> WhyTraitCodes);

public sealed record FutureJobsList(
    bool NeedsTest,
    IReadOnlyList<FutureJobRow> Items,
    string Peildatum);
