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

    /// <summary>
    /// Fit stays the first sort key, but two-decimal scores from a shared ISCO interest
    /// profile are near-ties. Jobs in the same 5-point band are treated as the same fit,
    /// so demand can decide inside the band.
    /// </summary>
    public const int FitBandWidth = 5;

    public static FutureJobsList Build(RiasecScores? scores, string? education = null)
    {
        var peildatum = OccupationOutlook.Shared.Peildatum;
        if (scores is not { IsComplete: true })
        {
            return new FutureJobsList(true, [], peildatum);
        }

        var eligible = new List<Occupation>();
        foreach (var job in OccupationCatalog.Shared.Listable)
        {
            if (IsEligible(job, scores, education))
            {
                eligible.Add(job);
            }
        }

        var rows = new List<FutureJobRow>();
        foreach (var group in eligible.GroupBy(job => IscoGroup(job.Isco)))
        {
            var job = ChooseRepresentative(group.ToList());
            if (job.Oi is not { Count: 6 } oi)
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

        var ordered = Order(rows);
        return new FutureJobsList(false, ordered, peildatum, SameWhy(ordered));
    }

    /// <summary>
    /// A job stays out when its interest profile is not sourced, its outlook is not sourced,
    /// employers will not strongly need it, ROA types the openings as low, it sits above the
    /// candidate's education, or it is a leadership title while leading is not one of their
    /// top three directions.
    /// </summary>
    public static bool IsEligible(Occupation job, RiasecScores scores, string? education = null)
    {
        if (scores is not { IsComplete: true } || !job.IsListable || job.Oi is not { Count: 6 })
        {
            return false;
        }

        if (!OccupationOutlook.Shared.TryGetSourcedDemand(job.Id, out var demand)
            || demand is null
            || demand.ItkbRank < StrongNeedMinRank
            || !OpeningsMatchStrongNeed(demand.OpeningsTypering))
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

    /// <summary>
    /// ITKB "groot" / "zeer groot" is the shortage. The openings typering is a different
    /// ROA figure. "Laag" and "erg laag" (16 per 100 or fewer) must not be shown under a
    /// "hard nodig" label.
    /// </summary>
    public static bool OpeningsMatchStrongNeed(string? openingsTypering)
    {
        var typering = (openingsTypering ?? "").Trim().ToLowerInvariant();
        return typering is "erg hoog" or "hoog" or "gemiddeld";
    }

    /// <summary>Plain Dutch for the ROA ITKB rank. Null when the rank is not a sourced typering.</summary>
    public static string? NeedLabelNl(int itkbRank) => itkbRank switch
    {
        4 => "Heel hard nodig",
        3 => "Hard nodig",
        2 => "Een beetje tekort",
        1 => "Bijna genoeg mensen",
        0 => "Genoeg mensen",
        _ => null
    };

    /// <summary>ISCO unit group. Codes longer than 4 digits use the first four.</summary>
    public static string IscoGroup(string? isco)
    {
        var code = (isco ?? "").Trim();
        return code.Length >= 4 ? code[..4] : code;
    }

    public static int FitLevel(decimal fitPercent)
    {
        if (fitPercent < 0m)
        {
            fitPercent = 0m;
        }
        else if (fitPercent > 100m)
        {
            fitPercent = 100m;
        }

        return (int)decimal.Floor(fitPercent / FitBandWidth);
    }

    /// <summary>
    /// One occupation per ISCO group. Prefer the Dutch name that CBS files under that same
    /// ISCO code. If several match, the shortest name is the general one. If none match,
    /// the shortest ESCO name wins, so "model" stays and "artistiek model" does not.
    /// </summary>
    public static Occupation ChooseRepresentative(IReadOnlyList<Occupation> group)
        => group
            .OrderByDescending(job => OccupationCatalog.Shared.IsCbsTitleForItsIsco(job))
            .ThenBy(job => DisplayTitle(job.Nl).Length)
            .ThenByDescending(job => string.Equals(job.Confidence, "high", StringComparison.OrdinalIgnoreCase))
            .ThenBy(job => DisplayTitle(job.Nl), StringComparer.OrdinalIgnoreCase)
            .First();

    /// <summary>Best fit band first. Inside a band: stronger ROA shortage, then more openings per 100.</summary>
    public static IReadOnlyList<FutureJobRow> Order(IEnumerable<FutureJobRow> rows)
        => rows
            .OrderByDescending(row => FitLevel(row.FitPercent))
            .ThenByDescending(row => row.ItkbRank)
            .ThenByDescending(row => row.OpeningsPer100)
            .ThenBy(row => row.TitleNl, StringComparer.OrdinalIgnoreCase)
            .Take(MaxCount)
            .ToList();

    /// <summary>True when every row names the same candidate interests, so the line can be shown once.</summary>
    public static bool SameWhy(IReadOnlyList<FutureJobRow> rows)
    {
        if (rows.Count < 2 || rows[0].WhyTraitCodes.Count == 0)
        {
            return false;
        }

        var first = string.Join("|", rows[0].WhyTraitCodes);
        for (var i = 1; i < rows.Count; i++)
        {
            if (!string.Equals(first, string.Join("|", rows[i].WhyTraitCodes), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Up to two of the candidate's own top three interests that this job's sourced profile
    /// also asks for, strongest job interest first. No fallback to a generic pair: a line
    /// that would be the same for every job is shown once above the list instead.
    /// </summary>
    public static IReadOnlyList<string> WhyTraitCodes(RiasecScores scores, IReadOnlyList<double> oi)
    {
        var top = RiasecRanking.Rank(
                scores.Realistic,
                scores.Investigative,
                scores.Artistic,
                scores.Social,
                scores.Enterprising,
                scores.Conventional)
            .Take(3)
            .Select(item => item.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matched = new List<(string Code, decimal Weight, int Index)>();
        var limit = Math.Min(6, oi.Count);
        for (var i = 0; i < limit; i++)
        {
            var weight = (decimal)oi[i] - 1m;
            if (weight <= 0m)
            {
                continue;
            }

            var code = CareerTestCatalog.RiasecCodes[i];
            if (top.Contains(code))
            {
                matched.Add((code, weight, i));
            }
        }

        return matched
            .OrderByDescending(item => item.Weight)
            .ThenBy(item => item.Index)
            .Take(2)
            .Select(item => item.Code)
            .ToList();
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
            0 => "",
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
    string Peildatum,
    bool WhyIsShared = false);
