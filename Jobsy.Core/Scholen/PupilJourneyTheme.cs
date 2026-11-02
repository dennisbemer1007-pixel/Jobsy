using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Visual journey theme for groep 7/8 (same questions, different world).
/// Keys match approved mockups (<c>zee</c>, <c>tuin</c>, <c>ster</c>).
/// </summary>
public static class PupilJourneyTheme
{
    public const string ZeeExpeditie = "zee";
    public const string Eilandtuin = "tuin";
    public const string Sterrenkust = "ster";

    public static readonly IReadOnlyList<string> All = [ZeeExpeditie, Eilandtuin, Sterrenkust];

    public static bool IsValid(string? key)
        => !string.IsNullOrWhiteSpace(key)
           && All.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string key)
    {
        var match = All.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new ArgumentOutOfRangeException(nameof(key), "Unknown journey theme.");
    }

    public static string NormalizeOrDefault(string? key)
        => IsValid(key) ? Normalize(key!) : ZeeExpeditie;

    public static string ShellCssClass(string? key)
        => IsValid(key) ? "ll-theme-" + Normalize(key!) : string.Empty;

    public static string Groep78ShellClass(string? key) => "t-" + NormalizeOrDefault(key);

    /// <summary>Short node label on the journey strip (mockup <c>jrow</c>, no “Het/De”).</summary>
    public static string StripTitleKey(string? themeKey, PupilGroep78JourneyNode node)
    {
        _ = NormalizeOrDefault(themeKey);
        if (node.Kind == PupilGroep78JourneyNodeKind.Puzzle)
        {
            return node.Key.ToLowerInvariant() switch
            {
                "schelpenrij" => "Leerling.Puzzle.Schelpenrij.Short",
                "schatkaart" => "Leerling.Puzzle.Schatkaart.Short",
                "vuurtorenlampen" => "Leerling.G78.Strip.lampen",
                _ => "Leerling.Puzzle.Title",
            };
        }

        if (node.Kind == PupilGroep78JourneyNodeKind.Result)
        {
            return "LeerlingStory.Title";
        }

        var w = node.Key.ToLowerInvariant();
        return $"Leerling.G78.Strip.{w}";
    }

    public static string WorldTitleKey(string? themeKey, string worldKey)
    {
        themeKey = NormalizeOrDefault(themeKey);
        if (string.Equals(worldKey, "pauze-eiland", StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.World.Island.Title";
        }

        return (themeKey, worldKey.ToLowerInvariant()) switch
        {
            (Eilandtuin, "koraalrif") => "Leerling.Theme.Tuin.Reef.Title",
            (Eilandtuin, "schatgrot") => "Leerling.Theme.Tuin.Cave.Title",
            (Eilandtuin, "vuurtoren") => "Leerling.Theme.Tuin.Lighthouse.Title",
            (Eilandtuin, "lagune") => "Leerling.Theme.Tuin.Lagoon.Title",
            (Sterrenkust, "koraalrif") => "Leerling.Theme.Ster.Reef.Title",
            (Sterrenkust, "schatgrot") => "Leerling.Theme.Ster.Cave.Title",
            (Sterrenkust, "vuurtoren") => "Leerling.Theme.Ster.Lighthouse.Title",
            (Sterrenkust, "lagune") => "Leerling.Theme.Ster.Lagoon.Title",
            _ => PupilWorldCatalog.Find(worldKey)?.TitleKey ?? "Leerling.World.Reef.Title",
        };
    }

    public static string WorldSubtitleKey(string? themeKey, string worldKey)
    {
        themeKey = NormalizeOrDefault(themeKey);
        if (string.Equals(worldKey, "pauze-eiland", StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.World.Island.Sub";
        }

        return themeKey switch
        {
            Eilandtuin => "Leerling.Theme.Tuin.World.Sub",
            Sterrenkust => "Leerling.Theme.Ster.World.Sub",
            _ => PupilWorldCatalog.Find(worldKey)?.SubtitleKey ?? "Leerling.World.Reef.Sub",
        };
    }

    public static string WorldBandImageStem(string? themeKey, string worldKey)
    {
        themeKey = NormalizeOrDefault(themeKey);
        var w = worldKey.ToLowerInvariant();
        if (w == "pauze-eiland")
        {
            return themeKey switch
            {
                Sterrenkust => "sterren-strand",
                Eilandtuin => "tuin-baai",
                _ => "eiland",
            };
        }

        return (themeKey, w) switch
        {
            (Eilandtuin, "koraalrif") => "tuin-koraal",
            (Eilandtuin, "schatgrot") => "grot",
            (Eilandtuin, "vuurtoren") => "vuurtoren",
            (Eilandtuin, "lagune") => "tuin-baai",
            (Sterrenkust, "koraalrif") => "sterren-strand",
            (Sterrenkust, "schatgrot") => "sterren-strand",
            (Sterrenkust, "vuurtoren") => "sterren-strand",
            (Sterrenkust, "lagune") => "sterren-strand",
            (ZeeExpeditie, "koraalrif") => "rif",
            (ZeeExpeditie, "schatgrot") => "grot",
            (ZeeExpeditie, "vuurtoren") => "vuurtoren",
            (ZeeExpeditie, "lagune") => "lagune",
            _ => themeKey switch
            {
                Sterrenkust => "sterren-strand",
                Eilandtuin => "tuin-baai",
                _ => "rif",
            },
        };
    }

    /// <summary>Maps progress world keys that are puzzle route ids back to a journey world for band art.</summary>
    public static string BandWorldKeyForProgress(string? currentWorldKey, string? puzzleKey = null)
    {
        var w = (currentWorldKey ?? "").Trim();
        if (IsPuzzleRouteKey(w))
        {
            w = "";
        }

        if (string.IsNullOrEmpty(w) && !string.IsNullOrWhiteSpace(puzzleKey))
        {
            w = "koraalrif";
        }

        return string.IsNullOrEmpty(w) ? "koraalrif" : w;
    }

    public static bool IsPuzzleRouteKey(string? key)
        => key?.Trim().ToLowerInvariant() is "schelpenrij" or "schatkaart" or "vuurtorenlampen";

    public static string WorldBandImagePath(string? themeKey, string worldKey)
        => $"/leerling/werelden/img/{WorldBandImageStem(themeKey, worldKey)}.jpg";

    public static string PuzzleTitleKey(string? themeKey, string puzzleKey)
    {
        var puzzle = puzzleKey.Trim().ToLowerInvariant();
        themeKey = NormalizeOrDefault(themeKey);
        return (themeKey, puzzle) switch
        {
            (Eilandtuin, "schelpenrij") => "Leerling.Puzzle.Tuin.Schelpenrij.Title",
            (Eilandtuin, "schatkaart") => "Leerling.Puzzle.Tuin.Schatkaart.Title",
            (Eilandtuin, "vuurtorenlampen") => "Leerling.Puzzle.Tuin.Vuurtoren.Title",
            (Sterrenkust, "schelpenrij") => "Leerling.Puzzle.Ster.Schelpenrij.Title",
            (Sterrenkust, "schatkaart") => "Leerling.Puzzle.Ster.Schatkaart.Title",
            (Sterrenkust, "vuurtorenlampen") => "Leerling.Puzzle.Ster.Vuurtoren.Title",
            (_, "schelpenrij") => "Leerling.Puzzle.Schelpenrij.Title",
            (_, "schatkaart") => "Leerling.Puzzle.Schatkaart.Title",
            (_, "vuurtorenlampen") => "Leerling.Puzzle.Vuurtoren.Title",
            _ => "Leerling.Puzzle.Title",
        };
    }

    public static string PuzzleLeadKey(string? themeKey, string puzzleKey)
    {
        var puzzle = puzzleKey.Trim().ToLowerInvariant();
        themeKey = NormalizeOrDefault(themeKey);
        return (themeKey, puzzle) switch
        {
            (ZeeExpeditie, "schelpenrij") => "Leerling.G78.Puzzle.IntroSub.Zee",
            (Eilandtuin, "schelpenrij") => "Leerling.Puzzle.Tuin.Schelpenrij.Lead",
            (Eilandtuin, "schatkaart") => "Leerling.Puzzle.Tuin.Schatkaart.Lead",
            (Eilandtuin, "vuurtorenlampen") => "Leerling.Puzzle.Tuin.Vuurtoren.Lead",
            (Sterrenkust, "schelpenrij") => "Leerling.G78.Puzzle.IntroSub.Ster",
            (Sterrenkust, "schatkaart") => "Leerling.Puzzle.Ster.Schatkaart.Lead",
            (Sterrenkust, "vuurtorenlampen") => "Leerling.Puzzle.Ster.Vuurtoren.Lead",
            (_, "schelpenrij") => "Leerling.Puzzle.Schelpenrij.Lead",
            (_, "schatkaart") => "Leerling.Puzzle.Schatkaart.Lead",
            (_, "vuurtorenlampen") => "Leerling.Puzzle.Vuurtoren.Lead",
            _ => "Leerling.Puzzle.Lead",
        };
    }

    public static IReadOnlyList<string> PuzzleMatchPairIds(string? themeKey)
    {
        _ = NormalizeOrDefault(themeKey);
        return ["1", "2", "3", "1", "2", "3"];
    }

    public static string PuzzleSortAsset(string? themeKey, int size)
    {
        var theme = NormalizeOrDefault(themeKey);
        var clamped = Math.Clamp(size, 1, 4);
        var ext = theme == ZeeExpeditie ? "png" : "jpg";
        return $"/images/leerling/werelden/puzzles/{theme}/sort-{clamped}.{ext}";
    }

    public static string PuzzleMatchAsset(string? themeKey, string pairId)
    {
        var theme = NormalizeOrDefault(themeKey);
        var id = pairId.Trim();
        if (id is not ("1" or "2" or "3"))
        {
            id = "1";
        }

        return $"/images/leerling/werelden/puzzles/{theme}/match-{id}.jpg";
    }

    public static string PuzzleLampAsset(string? themeKey, int lampIndex)
    {
        var theme = NormalizeOrDefault(themeKey);
        var n = Math.Clamp(lampIndex + 1, 1, 3);
        return $"/images/leerling/werelden/puzzles/{theme}/lamp-{n}.jpg";
    }

    public static string PuzzleCardBackAsset(string? themeKey)
    {
        var theme = NormalizeOrDefault(themeKey);
        return $"/images/leerling/werelden/puzzles/{theme}/card-back.jpg";
    }

    public static (string Stem, string Position) ResultSceneBand(string? themeKey)
        => NormalizeOrDefault(themeKey) switch
        {
            Eilandtuin => ("dot-tuin-6", "center 55%"),
            Sterrenkust => ("dot-ster-5", "center 45%"),
            _ => ("lagune", "center 40%"),
        };

    public static IReadOnlyList<string> ResultTileStems(string? themeKey)
        => NormalizeOrDefault(themeKey) switch
        {
            Eilandtuin => ["dot-tuin-1", "dot-tuin-2", "dot-tuin-4", "dot-tuin-6"],
            Sterrenkust => ["dot-ster-1", "dot-ster-2", "dot-ster-4", "dot-ster-5"],
            _ => ["rif", "grot", "vuurtoren", "lagune"],
        };

    public static int PuzzleOrdinal(string puzzleKey)
        => puzzleKey.Trim().ToLowerInvariant() switch
        {
            "schelpenrij" => 1,
            "schatkaart" => 2,
            "vuurtorenlampen" => 3,
            _ => 1,
        };

    /// <summary>Fixed preview row for G78 schelpenrij (mockup <c>prev-row</c>).</summary>
    public static IReadOnlyList<int> SchelpenrijPatternSequence() => [1, 2, 2, 3, 1];

    /// <summary>Correct shell size after the sequence (mockup pattern).</summary>
    public static int SchelpenrijPatternAnswer() => 2;

    /// <summary>Five tap targets shown under the row (mockup <c>answers</c> grid).</summary>
    public static IReadOnlyList<int> SchelpenrijPatternChoices() => [1, 2, 3, 2, 1];

    public static string SchelpenrijIntroSubKey(string? themeKey)
        => NormalizeOrDefault(themeKey) switch
        {
            Eilandtuin => "Leerling.Puzzle.Tuin.Schelpenrij.Lead",
            Sterrenkust => "Leerling.G78.Puzzle.IntroSub.Ster",
            _ => "Leerling.G78.Puzzle.IntroSub.Zee",
        };

    public static IEnumerable<string> AllPuzzleAssetPaths()
    {
        foreach (var theme in All)
        {
            for (var size = 1; size <= 4; size++)
            {
                yield return PuzzleSortAsset(theme, size).TrimStart('/');
            }

            for (var pair = 1; pair <= 3; pair++)
            {
                yield return PuzzleMatchAsset(theme, pair.ToString()).TrimStart('/');
            }

            for (var lamp = 1; lamp <= 3; lamp++)
            {
                yield return PuzzleLampAsset(theme, lamp - 1).TrimStart('/');
            }

            yield return PuzzleCardBackAsset(theme).TrimStart('/');
        }
    }
}
