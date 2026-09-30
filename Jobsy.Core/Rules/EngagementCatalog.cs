namespace Jobsy.Core.Rules;

/// <summary>
/// Six maatschappelijke betrokkenheid items (D13). Ids are stable; labels via <c>WaEngage.*</c>.
/// </summary>
public static class EngagementCatalog
{
    public const int MaxBonus = 5;
    public const int DriverThreshold = 70;
    public const int SelfDeclaredPoints = 1;
    public const int CheckedPoints = 2;
    public const int MaxProofUrlLength = 500;
    public const int MaxProofTextLength = 300;
    public const int RemovedCooldownDays = 30;
    public const int MaxCardBadges = 2;

    public const string Duurzaam = "duurzaam";
    public const string WerkVoorIedereen = "werk-voor-iedereen";
    public const string Leerbedrijf = "leerbedrijf";
    public const string Lokaal = "lokaal";
    public const string Diversiteit = "diversiteit";
    public const string EerlijkLoon = "eerlijk-loon";

    public static readonly EngagementItemDefinition[] All =
    [
        new(Duurzaam, SchwartzValuesCatalog.Impact, "WaEngage.Item.duurzaam", "🌿", "WaEngage.Hint.duurzaam"),
        new(WerkVoorIedereen, SchwartzValuesCatalog.Impact, "WaEngage.Item.werk-voor-iedereen", "🤝", "WaEngage.Hint.werk-voor-iedereen"),
        new(Leerbedrijf, SchwartzValuesCatalog.Connection, "WaEngage.Item.leerbedrijf", "🎓", "WaEngage.Hint.leerbedrijf"),
        new(Lokaal, SchwartzValuesCatalog.Connection, "WaEngage.Item.lokaal", "🏘️", "WaEngage.Hint.lokaal"),
        new(Diversiteit, SchwartzValuesCatalog.Impact, "WaEngage.Item.diversiteit", "🌈", "WaEngage.Hint.diversiteit"),
        new(EerlijkLoon, SchwartzValuesCatalog.Impact, "WaEngage.Item.eerlijk-loon", "⚖️", "WaEngage.Hint.eerlijk-loon")
    ];

    private static readonly Dictionary<string, EngagementItemDefinition> ById =
        All.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string? id, out EngagementItemDefinition item)
    {
        item = null!;
        return !string.IsNullOrWhiteSpace(id) && ById.TryGetValue(id.Trim(), out item!);
    }

    public static bool IsKnown(string? id) => TryGet(id, out _);

    public static string? NormalizeId(string? id)
        => TryGet(id, out var item) ? item.Id : null;

    /// <summary>
    /// Match bonus: only when the candidate has a completed Waardentest.
    /// Per claimed item whose linked driver is ≥ 70: +1 SelfDeclared / +2 Checked.
    /// Cap +5; never negative.
    /// </summary>
    public static int ComputeBonus(
        SchwartzValuesScores? candidateValues,
        IEnumerable<CompanyEngagementMatchItem>? claims)
    {
        if (candidateValues is not { IsComplete: true } || claims is null)
        {
            return 0;
        }

        var total = 0;
        foreach (var claim in claims)
        {
            if (claim.Status == CompanyEngagementStatuses.Removed
                || !TryGet(claim.ItemId, out var def))
            {
                continue;
            }

            var driverScore = candidateValues.Get(def.DriverCode);
            if (driverScore < DriverThreshold)
            {
                continue;
            }

            total += claim.Status == CompanyEngagementStatuses.Checked
                ? CheckedPoints
                : SelfDeclaredPoints;
            if (total >= MaxBonus)
            {
                return MaxBonus;
            }
        }

        return Math.Clamp(total, 0, MaxBonus);
    }

    public static IReadOnlyList<string> MatchingItemLabelsNl(
        SchwartzValuesScores? candidateValues,
        IEnumerable<CompanyEngagementMatchItem>? claims)
    {
        if (candidateValues is not { IsComplete: true } || claims is null)
        {
            return [];
        }

        var labels = new List<string>();
        foreach (var claim in claims)
        {
            if (claim.Status == CompanyEngagementStatuses.Removed
                || !TryGet(claim.ItemId, out var def))
            {
                continue;
            }

            if (candidateValues.Get(def.DriverCode) < DriverThreshold)
            {
                continue;
            }

            labels.Add(DutchLabel(def.Id));
        }

        return labels;
    }

    public static string DutchLabel(string id) => id switch
    {
        Duurzaam => "duurzaamheid",
        WerkVoorIedereen => "werk voor iedereen",
        Leerbedrijf => "erkend leerbedrijf",
        Lokaal => "lokaal betrokken",
        Diversiteit => "diversiteit en inclusie",
        EerlijkLoon => "eerlijk loon en cao",
        _ => id
    };
}

public sealed record EngagementItemDefinition(
    string Id,
    string DriverCode,
    string TitleKey,
    string Emoji,
    string HintKey);

/// <summary>Lightweight claim for match scoring / discovery (no proof text).</summary>
public sealed record CompanyEngagementMatchItem(
    string ItemId,
    string Status);

public static class CompanyEngagementStatuses
{
    public const string SelfDeclared = "SelfDeclared";
    public const string Checked = "Checked";
    public const string Removed = "Removed";

    public static bool IsPublic(string? status)
        => status is SelfDeclared or Checked;
}

public static class CompanyEngagementCheckedSources
{
    public const string Admin = "Admin";
    public const string Sbb = "Sbb";
}
