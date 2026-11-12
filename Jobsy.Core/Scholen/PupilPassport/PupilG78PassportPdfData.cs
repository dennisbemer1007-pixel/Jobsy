using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.PupilPassport;

public sealed record PupilPassportValueBar(string Label, int ScorePercent);

/// <summary>Fact-only G78 bijlagen data for PDF (RIASEC, values, jobs).</summary>
public static class PupilG78PassportPdfData
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static (IReadOnlyList<PupilPassportRiasecBar> Bars, bool Pending) BuildRiasecBars(PupilResult result)
    {
        var scores = ParseRiasec(result.RiasecScoresJson);
        if (scores is null || !HasRiasecMeasurements(scores))
        {
            return ([], true);
        }

        var letters = new[] { 'R', 'I', 'A', 'S', 'E', 'C' };
        var bars = letters
            .Select(c =>
            {
                var code = RiasecCategory(c);
                var score = scores.Get(code);
                return new PupilPassportRiasecBar(
                    c.ToString(),
                    PupilPassportCopy.Get($"LeerlingPassport.G78.Riasec.{c}"),
                    score);
            })
            .ToList();

        if (bars.All(b => b.Score <= 0))
        {
            return ([], true);
        }

        return (bars, false);
    }

    /// <summary>Top story competencies with scores from the same Likert averages as the fact story.</summary>
    public static IReadOnlyList<PupilPassportRiasecBar> BuildCompetencyEnergyBars(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlyList<string> storyStrongCategories)
    {
        if (storyStrongCategories.Count == 0)
        {
            return [];
        }

        var avgs = PupilG78GrowthCompetencies.DimensionAverages(answers);
        return storyStrongCategories
            .Take(3)
            .Select(cat =>
            {
                var mean = avgs.TryGetValue(cat, out var m) ? m : 3.0;
                var score = (int)Math.Clamp(Math.Round(mean / 5.0 * 100.0, MidpointRounding.AwayFromZero), 0, 100);
                return new PupilPassportRiasecBar(
                    CompetencyGlyphLetter(cat),
                    PupilG78CompetencyNarrative.KidLabelTitle(cat),
                    score,
                    CompetencyBarDescription(mean));
            })
            .ToList();
    }

    private static string CompetencyBarDescription(double mean) => mean switch
    {
        >= 4.25 => "Dit komt in je antwoorden duidelijk naar voren.",
        >= 3.5 => "Dit zie je ook terug in je antwoorden.",
        >= 3.0 => "Soms komt dit in je antwoorden naar voren.",
        _ => "Dit komt in je antwoorden minder naar voren."
    };

    private static string CompetencyGlyphLetter(string category) => category switch
    {
        CompetencyTestCatalog.Innovatie => "A",
        CompetencyTestCatalog.Samenwerken => "S",
        CompetencyTestCatalog.Extraversie => "E",
        CompetencyTestCatalog.Resultaatgerichtheid => "C",
        CompetencyTestCatalog.Stressbestendigheid => "R",
        _ => "S"
    };

    public static IReadOnlyList<PupilPassportValueBar> BuildValueBars(PupilResult result)
    {
        var values = ParseValues(result.ValuesScoresJson);
        if (values is null)
        {
            return [];
        }

        return SchwartzValuesCatalog.CategoryCodes
            .Select(code => (Code: code, Score: ScoreOrNull(values, code)))
            .Where(x => x.Score is > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => new PupilPassportValueBar(
                SchwartzValuesCatalog.EverydayLabel(x.Code),
                x.Score!.Value))
            .ToList();
    }

    internal static RiasecScores? ParseRiasec(string? json) => Deserialize<RiasecScores>(json);

    internal static SchwartzValuesScores? ParseValues(string? json) => Deserialize<SchwartzValuesScores>(json);

    private static bool HasRiasecMeasurements(RiasecScores scores)
        => scores.TryGet(CareerTestCatalog.Realistic) is not null
           || scores.TryGet(CareerTestCatalog.Investigative) is not null
           || scores.TryGet(CareerTestCatalog.Artistic) is not null
           || scores.TryGet(CareerTestCatalog.Social) is not null
           || scores.TryGet(CareerTestCatalog.Enterprising) is not null
           || scores.TryGet(CareerTestCatalog.Conventional) is not null;

    private static string RiasecCategory(char letter) => letter switch
    {
        'R' => CareerTestCatalog.Realistic,
        'I' => CareerTestCatalog.Investigative,
        'A' => CareerTestCatalog.Artistic,
        'S' => CareerTestCatalog.Social,
        'E' => CareerTestCatalog.Enterprising,
        _ => CareerTestCatalog.Conventional
    };

    private static int? ScoreOrNull(SchwartzValuesScores scores, string code) => code switch
    {
        SchwartzValuesCatalog.Autonomy => scores.Autonomy,
        SchwartzValuesCatalog.Connection => scores.Connection,
        SchwartzValuesCatalog.Achievement => scores.Achievement,
        SchwartzValuesCatalog.Stability => scores.Stability,
        SchwartzValuesCatalog.Impact => scores.Impact,
        _ => null
    };

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json.Trim(), "{}", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
