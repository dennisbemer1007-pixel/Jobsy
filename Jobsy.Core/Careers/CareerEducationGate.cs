using Jobsy.Core.Rules;

namespace Jobsy.Core.Careers;

/// <summary>
/// ISCO skill-level gate for a known education. Unknown or empty education is no gate.
/// havo and vwo without a further diploma are treated as mbo 4 (max level 3); that choice is open for review.
/// </summary>
public static class CareerEducationGate
{
    public readonly record struct Result(int? MaxLevel, bool FlagLevel3)
    {
        public static Result Open { get; } = new(null, false);

        public bool HasGate => MaxLevel is not null;

        /// <summary>Approved level substitution applies up to and including mbo 4.</summary>
        public bool SubstituteLowerOffice => MaxLevel is < 4;
    }

    public static Result MaxIscoLevel(string? education)
    {
        var fold = CareerOccupationKeys.Fold(education ?? "");
        if (fold.Length == 0)
        {
            return Result.Open;
        }

        var tokens = fold.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Contains("wo") || tokens.Any(t => t.StartsWith("universit", StringComparison.Ordinal)))
        {
            return new Result(4, false);
        }

        if (tokens.Contains("hbo"))
        {
            return new Result(4, false);
        }

        if (fold.Contains("mbo 4", StringComparison.Ordinal) || fold.Contains("mbo4", StringComparison.Ordinal))
        {
            return new Result(3, false);
        }

        if (fold.Contains("mbo 3", StringComparison.Ordinal) || fold.Contains("mbo3", StringComparison.Ordinal)
            || fold.Contains("mbo 2", StringComparison.Ordinal) || fold.Contains("mbo2", StringComparison.Ordinal))
        {
            return new Result(2, true);
        }

        if (fold.Contains("mbo 1", StringComparison.Ordinal) || fold.Contains("mbo1", StringComparison.Ordinal))
        {
            return new Result(2, false);
        }

        if (tokens.Any(t => t.StartsWith("mbo", StringComparison.Ordinal)))
        {
            return new Result(2, true);
        }

        // Not in the approved map. Treated as mbo 4 so the gate is not wider than mbo 4.
        if (tokens.Contains("havo") || tokens.Contains("vwo"))
        {
            return new Result(3, false);
        }

        if (tokens.Contains("vmbo")
            || tokens.Contains("mavo")
            || tokens.Contains("praktijkonderwijs")
            || tokens.Contains("basisonderwijs")
            || tokens.Contains("basis")
            || tokens.Contains("geen"))
        {
            return new Result(2, false);
        }

        return Result.Open;
    }

    public static bool Passes(int? iscoLevel, Result gate)
    {
        if (gate.MaxLevel is not int max || iscoLevel is not int level)
        {
            return true;
        }

        if (level <= max)
        {
            return true;
        }

        return gate.FlagLevel3 && level == 3;
    }

    public static bool NeedsExtraTraining(int? iscoLevel, Result gate)
        => gate.FlagLevel3 && iscoLevel == 3 && gate.MaxLevel is < 3;
}
