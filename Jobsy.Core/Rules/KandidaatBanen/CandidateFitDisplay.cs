namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>
/// Pure candidate-side display layer (D2). Does not change employer scores /
/// <see cref="ProfileVacancyMatch.TotalPercent"/> / snapshot version.
/// </summary>
public static class CandidateFitDisplay
{
    public const int MinPercent = 55;
    public const int MaxPercent = 90;
    public const int StrongThreshold = 75;
    public const int GoodThreshold = 65;

    /// <summary>DNA dimension weights (renormalised over available dims).</summary>
    public const double WeightCulture = 0.30;
    public const double WeightValues = 0.25;
    public const double WeightCompetencies = 0.25;
    public const double WeightInterests = 0.20;

    public const double WeightDna = 0.50;
    public const double WeightPractical = 0.50;

    /// <summary>
    /// Piecewise-linear anchors mapping composition score <c>s</c> (0–1) → display percent.
    /// Tuned without <c>ICompanyCultureLookup</c> (werkgever-aanmelding 01 ABSENT) — re-run
    /// <c>CandidateFitDistributionReportTests</c> when that lands.
    /// </summary>
    public static IReadOnlyList<(double S, int Percent)> Anchors { get; } =
    [
        (0.00, 55),
        (0.50, 56),
        (0.60, 58),
        (0.68, 61),
        (0.74, 65),
        (0.79, 69),
        (0.84, 74),
        (0.88, 78),
        (0.92, 83),
        (0.96, 87),
        (1.00, 90)
    ];

    /// <summary>
    /// Builds candidate fit, or <c>null</c> when the gate is closed / anonymous.
    /// </summary>
    public static CandidateFit? Build(ProfileVacancyMatch match, CandidateFitGate gate)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (!gate.IsOpen)
        {
            return null;
        }

        var dimensions = BuildDimensions(match);
        var s = CompositionScore(match, dimensions);
        var percent = MapToPercent(s);
        var band = BandFor(percent);
        var whyKinds = MapWhyKinds(match);

        return new CandidateFit
        {
            Percent = percent,
            Band = band,
            WhyKinds = whyKinds,
            Dimensions = dimensions
        };
    }

    public static KbFitBand BandFor(int percent)
    {
        if (percent >= StrongThreshold)
        {
            return KbFitBand.Strong;
        }

        if (percent >= GoodThreshold)
        {
            return KbFitBand.Good;
        }

        return KbFitBand.Some;
    }

    public static int MapToPercent(double s)
    {
        s = Math.Clamp(s, 0, 1);
        var anchors = Anchors;
        if (s <= anchors[0].S)
        {
            return anchors[0].Percent;
        }

        for (var i = 1; i < anchors.Count; i++)
        {
            var (s1, p1) = anchors[i - 1];
            var (s2, p2) = anchors[i];
            if (s <= s2)
            {
                var t = (s2 - s1) <= 1e-9 ? 1 : (s - s1) / (s2 - s1);
                var raw = p1 + t * (p2 - p1);
                return (int)Math.Clamp(
                    Math.Round(raw, MidpointRounding.AwayFromZero),
                    MinPercent,
                    MaxPercent);
            }
        }

        return MaxPercent;
    }

    /// <summary>Composition score s ∈ [0,1] used by the piecewise map.</summary>
    public static double CompositionScore(ProfileVacancyMatch match, CandidateFitDimensions? dimensions = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        dimensions ??= BuildDimensions(match);

        var dna = DnaPart(dimensions);
        var practical = PracticalPart(match.Core);
        if (dna is null)
        {
            return practical;
        }

        return WeightDna * dna.Value + WeightPractical * practical;
    }

    public static CandidateFitDimensions BuildDimensions(ProfileVacancyMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);

        int? culture = match.CultureFit?.Percent;
        if (culture is null && match.CultureDim01 is double c01)
        {
            culture = ToPercent(c01);
        }

        int? values = match.ValuesFit01 is double v01 ? ToPercent(v01) : null;
        int? competencies = match.CompetencyDim01 is double d01
            ? ToPercent(d01)
            : match.CompetencyScore01 is double csc ? ToPercent(csc) : null;
        int? interests = match.InterestScore01 is double i01 ? ToPercent(i01) : null;

        return new CandidateFitDimensions(culture, values, competencies, interests);
    }

    /// <summary>
    /// Maps the first MatchWhy points to short candidate why-kind codes (<c>Kb.Why.*</c>).
    /// At most two distinct fragments. Empty when no usable why data.
    /// </summary>
    public static IReadOnlyList<string> MapWhyKinds(ProfileVacancyMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);
        var kinds = new List<string>(2);
        foreach (var point in match.Why)
        {
            var mapped = MapWhyKind(point.Kind);
            if (mapped is null || kinds.Contains(mapped, StringComparer.Ordinal))
            {
                continue;
            }

            kinds.Add(mapped);
            if (kinds.Count >= 2)
            {
                break;
            }
        }

        if (kinds.Count == 0 && match.CultureFit is { Why: { Length: > 0 } })
        {
            kinds.Add("culture");
        }

        return kinds;
    }

    /// <summary>Joins localized why fragments with " · ". Returns null when empty.</summary>
    public static string? FormatWhyLine(IReadOnlyList<string> whyKinds, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(whyKinds);
        ArgumentNullException.ThrowIfNull(localize);
        if (whyKinds.Count == 0)
        {
            return null;
        }

        var parts = new List<string>(whyKinds.Count);
        foreach (var kind in whyKinds)
        {
            var text = localize($"Kb.Why.{kind}");
            if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("Kb.Why.", StringComparison.Ordinal))
            {
                parts.Add(text.Trim());
            }
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? MapWhyKind(string? kind) => kind switch
    {
        "culture" => "culture",
        "values" => "values",
        "competency" => "competency",
        "interest" or "occupation" => "interest",
        "travel" => "travel",
        "hours" or "dayparts" => "hours",
        "experience" => "competency",
        _ => null
    };

    private static double? DnaPart(CandidateFitDimensions d)
    {
        double sum = 0;
        double weight = 0;
        void Add(int? pct, double w)
        {
            if (pct is not int p)
            {
                return;
            }

            sum += w * (Math.Clamp(p, 0, 100) / 100.0);
            weight += w;
        }

        Add(d.Culture, WeightCulture);
        Add(d.Values, WeightValues);
        Add(d.Competencies, WeightCompetencies);
        Add(d.Interests, WeightInterests);

        if (weight <= 1e-9)
        {
            return null;
        }

        return sum / weight;
    }

    private static double PracticalPart(MatchScoreBreakdown core)
    {
        var travel = Ratio(core.TravelScore, MatchScoreWeights.Travel);
        var hours = Ratio(core.HoursScore, MatchScoreWeights.Hours);
        var dayParts = Ratio(core.DayPartsScore, MatchScoreWeights.DayParts);
        // Equal weights across the three practical components.
        return (travel + hours + dayParts) / 3.0;
    }

    private static double Ratio(int points, int weight)
        => weight <= 0 ? 0 : Math.Clamp(points / (double)weight, 0, 1);

    private static int ToPercent(double score01)
        => (int)Math.Clamp(Math.Round(100 * score01, MidpointRounding.AwayFromZero), 0, 100);
}
