using System.Text.Json;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Ten kernwaarden cards (2 per Schwartz driver). Employers pick exactly 3.
/// Scores: driver with 2 cards = 90, 1 card = 75, 0 cards = 40.
/// </summary>
public static class CompanyValueCards
{
    public const int RequiredCount = 3;
    public const int TwoCardsPercent = 90;
    public const int OneCardPercent = 75;
    public const int ZeroCardsPercent = 40;

    public static readonly CompanyValueCardDefinition[] All =
    [
        new("vrijheid", SchwartzValuesCatalog.Autonomy, "WaProfile.Card.vrijheid", "🕊️"),
        new("uitdaging", SchwartzValuesCatalog.Autonomy, "WaProfile.Card.uitdaging", "🔍"),
        new("teamgevoel", SchwartzValuesCatalog.Connection, "WaProfile.Card.teamgevoel", "🤝"),
        new("zorg", SchwartzValuesCatalog.Connection, "WaProfile.Card.zorg", "🤗"),
        new("groei", SchwartzValuesCatalog.Achievement, "WaProfile.Card.groei", "🛠️"),
        new("resultaat", SchwartzValuesCatalog.Achievement, "WaProfile.Card.resultaat", "🏆"),
        new("zekerheid", SchwartzValuesCatalog.Stability, "WaProfile.Card.zekerheid", "⚓"),
        new("vakmanschap", SchwartzValuesCatalog.Stability, "WaProfile.Card.vakmanschap", "📋"),
        new("betekenis", SchwartzValuesCatalog.Impact, "WaProfile.Card.betekenis", "🌍"),
        new("eerlijk", SchwartzValuesCatalog.Impact, "WaProfile.Card.eerlijk", "🌱")
    ];

    private static readonly Dictionary<string, CompanyValueCardDefinition> ById =
        All.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string? id, out CompanyValueCardDefinition card)
    {
        card = null!;
        return !string.IsNullOrWhiteSpace(id) && ById.TryGetValue(id.Trim(), out card!);
    }

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? raw)
    {
        if (raw is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>(RequiredCount);
        foreach (var item in raw)
        {
            if (!TryGet(item, out var card) || !seen.Add(card.Id))
            {
                continue;
            }

            list.Add(card.Id);
            if (list.Count == RequiredCount)
            {
                break;
            }
        }

        return list;
    }

    /// <summary>Valid when empty (skipped) or exactly 3 known cards.</summary>
    public static bool IsValidSelection(IEnumerable<string>? raw)
    {
        if (raw is null)
        {
            return true;
        }

        var provided = raw
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
        if (provided.Count == 0)
        {
            return true;
        }

        if (provided.Count != RequiredCount)
        {
            return false;
        }

        var normalized = Normalize(provided);
        return normalized.Count == RequiredCount;
    }

    public static string Serialize(IEnumerable<string>? raw)
        => JsonSerializer.Serialize(Normalize(raw));

    public static IReadOnlyList<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var ids = JsonSerializer.Deserialize<string[]>(json) ?? [];
            return Normalize(ids);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static SchwartzValuesScores Score(IEnumerable<string>? cardIds)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            counts[code] = 0;
        }

        foreach (var id in Normalize(cardIds))
        {
            if (TryGet(id, out var card))
            {
                counts[card.DriverCode] = counts.GetValueOrDefault(card.DriverCode) + 1;
            }
        }

        int Pct(string code) => counts.GetValueOrDefault(code) switch
        {
            >= 2 => TwoCardsPercent,
            1 => OneCardPercent,
            _ => ZeroCardsPercent
        };

        return new SchwartzValuesScores(
            Autonomy: Pct(SchwartzValuesCatalog.Autonomy),
            Connection: Pct(SchwartzValuesCatalog.Connection),
            Achievement: Pct(SchwartzValuesCatalog.Achievement),
            Stability: Pct(SchwartzValuesCatalog.Stability),
            Impact: Pct(SchwartzValuesCatalog.Impact));
    }

    public static void ApplyScores(CompanyValuesProfile row, IEnumerable<string> cardIds)
    {
        var scores = Score(cardIds);
        row.CardIdsJson = Serialize(cardIds);
        row.AutonomyPercent = scores.Autonomy ?? ZeroCardsPercent;
        row.ConnectionPercent = scores.Connection ?? ZeroCardsPercent;
        row.AchievementPercent = scores.Achievement ?? ZeroCardsPercent;
        row.StabilityPercent = scores.Stability ?? ZeroCardsPercent;
        row.ImpactPercent = scores.Impact ?? ZeroCardsPercent;
    }

    public static SchwartzValuesScores ToScores(CompanyValuesProfile row)
        => new(
            Autonomy: row.AutonomyPercent,
            Connection: row.ConnectionPercent,
            Achievement: row.AchievementPercent,
            Stability: row.StabilityPercent,
            Impact: row.ImpactPercent);
}

public sealed record CompanyValueCardDefinition(
    string Id,
    string DriverCode,
    string TitleKey,
    string Emoji);
