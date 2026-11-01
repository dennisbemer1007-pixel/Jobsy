using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>JSON stored on <see cref="Entities.Scholen.PupilProgress.MboHboProgressJson"/>.</summary>
public sealed class PupilMboHboProgressState
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool IntroSeen { get; set; }

    /// <summary>Single-scale answers (interesse, competentie, beroepen, werkwaarden).</summary>
    public Dictionary<string, int> Answers { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Vakken: id → leuk/goed 1–5.</summary>
    public Dictionary<string, PupilMboHboVakScore> VakScores { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Block key → UTC completion timestamp (ISO).</summary>
    public Dictionary<string, string> BlocksCompleted { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static PupilMboHboProgressState Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PupilMboHboProgressState();
        }

        try
        {
            return JsonSerializer.Deserialize<PupilMboHboProgressState>(json, JsonOptions)
                   ?? new PupilMboHboProgressState();
        }
        catch (JsonException)
        {
            return new PupilMboHboProgressState();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static string BlockKey(PupilMboHboBlock block)
        => block switch
        {
            PupilMboHboBlock.Vakken => "vakken",
            PupilMboHboBlock.Interesse => "interesse",
            PupilMboHboBlock.Competentie => "competentie",
            PupilMboHboBlock.Beroepen => "beroepen",
            PupilMboHboBlock.Werkwaarden => "werkwaarden",
            _ => block.ToString().ToLowerInvariant()
        };

    public bool IsBlockComplete(PupilMboHboBlock block)
        => BlocksCompleted.ContainsKey(BlockKey(block));

    public int AnsweredInBlock(PupilMboHboBlock block, PupilMboHboCatalog catalog, Enums.SchoolLevel level)
    {
        if (block == PupilMboHboBlock.Vakken)
        {
            return VakScores.Count;
        }

        var total = catalog.QuestionCount(block, level);
        var prefix = block switch
        {
            PupilMboHboBlock.Interesse => "i",
            PupilMboHboBlock.Competentie => "c",
            PupilMboHboBlock.Beroepen => "b",
            PupilMboHboBlock.Werkwaarden => "w",
            _ => ""
        };
        return Answers.Keys.Count(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }

    public bool BlockFullyAnswered(PupilMboHboBlock block, PupilMboHboCatalog catalog, Enums.SchoolLevel level)
    {
        var questions = catalog.QuestionsFor(block, level);
        if (block == PupilMboHboBlock.Vakken)
        {
            foreach (var q in questions)
            {
                if (!VakScores.TryGetValue(q.Id, out var score)
                    || !LikertAnswerJson.IsValidAnswer(score.Leuk)
                    || !LikertAnswerJson.IsValidAnswer(score.Goed))
                {
                    return false;
                }
            }

            return questions.Count > 0;
        }

        foreach (var q in questions)
        {
            if (!Answers.TryGetValue(q.Id, out var v) || !LikertAnswerJson.IsValidAnswer(v))
            {
                return false;
            }
        }

        return questions.Count > 0;
    }

    public int CompletedBlockCount()
        => BlocksCompleted.Count;

    public bool AllBlocksComplete()
        => BlocksCompleted.Count >= PupilMboHboCatalog.AllBlocks.Count
           && PupilMboHboCatalog.AllBlocks.All(IsBlockComplete);
}

public sealed class PupilMboHboVakScore
{
    public int Leuk { get; set; }
    public int Goed { get; set; }
}
