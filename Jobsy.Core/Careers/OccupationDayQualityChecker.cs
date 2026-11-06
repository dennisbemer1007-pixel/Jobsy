using System.Text.RegularExpressions;

namespace Jobsy.Core.Careers;

/// <summary>Fast checks before a generated day is stored. Failures are passed back into a retry prompt.</summary>
public static partial class OccupationDayQualityChecker
{
    public static bool TryCheck(
        OccupationDayDraft draft,
        OccupationDayFacts facts,
        out IReadOnlyList<string> reasons)
    {
        var found = new List<string>();
        var workplace = OccupationDaySourceRewrite.DetectWorkplace(facts.TitleNl, facts.Description, facts.AltNames);
        var blob = FullText(draft);

        if (draft.Blocks is { Count: > 0 } blocks)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                var label = (blocks[i].Label ?? "").Trim();
                if (!LabelHasClock(label))
                {
                    found.Add("blok " + (i + 1) + ": geen kloktijd in label");
                }
            }
        }

        foreach (Match match in AdjacentDuplicateWord().Matches(blob))
        {
            found.Add("herhaling: " + match.Groups[1].Value);
            break;
        }

        foreach (Match match in AdjacentDuplicateComma().Matches(blob))
        {
            found.Add("herhaling: " + match.Groups[1].Value);
            break;
        }

        if (blob.Contains("keukenhulpverlener", StringComparison.OrdinalIgnoreCase))
        {
            found.Add("woord: keukenhulpverlener");
        }

        if (workplace == OccupationDaySourceRewrite.WorkplaceKind.Greenhouse
            && (blob.Contains("tuin schoonmaken", StringComparison.OrdinalIgnoreCase)
                || blob.Contains("tuinen schoonmaken", StringComparison.OrdinalIgnoreCase)
                || blob.Contains("schoonmaken van tuinen", StringComparison.OrdinalIgnoreCase)))
        {
            found.Add("onderwerp: tuin i.p.v. kas");
        }

        if (workplace == OccupationDaySourceRewrite.WorkplaceKind.Greenhouse
            && blob.Contains("hovenier", StringComparison.OrdinalIgnoreCase))
        {
            found.Add("onderwerp: hovenier");
        }

        if (workplace == OccupationDaySourceRewrite.WorkplaceKind.RoadTransport
            && blob.Contains("tuin", StringComparison.OrdinalIgnoreCase)
            && !facts.SourceText.Contains("tuin", StringComparison.OrdinalIgnoreCase))
        {
            found.Add("onderwerp: tuin");
        }

        reasons = found.Distinct(StringComparer.Ordinal).Take(6).ToList();
        return reasons.Count == 0;
    }

    public static bool LabelHasClock(string? label)
    {
        var text = (label ?? "").Trim();
        if (text.Length == 0)
        {
            return false;
        }

        return ClockInLabel().IsMatch(text);
    }

    private static string FullText(OccupationDayDraft draft)
        => string.Join(
            "\n",
            draft.Morning,
            draft.Midday,
            draft.Afternoon,
            draft.Closing,
            string.Join("\n", draft.Highlights),
            string.Join("\n", (draft.Blocks ?? []).Select(b => b.Label + " " + b.Text)),
            draft.VariesNote);

    [GeneratedRegex(@"\b([01]?\d|2[0-3])[:.][0-5]\d\b", RegexOptions.CultureInvariant)]
    private static partial Regex ClockInLabel();

    [GeneratedRegex(@"\b([\p{L}]{3,})\s*[,;]\s*\1\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdjacentDuplicateComma();

    [GeneratedRegex(@"\b([\p{L}]{3,})\s+\1\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdjacentDuplicateWord();
}
