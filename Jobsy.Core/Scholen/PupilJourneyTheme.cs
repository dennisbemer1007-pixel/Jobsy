using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Visual journey theme for groep 7/8. Logic world keys stay the same; only copy and styling differ.
/// </summary>
public static class PupilJourneyTheme
{
    public const string Zee = "zee";
    public const string Tuin = "tuin";
    public const string Ster = "ster";

    public static readonly IReadOnlyList<string> All = [Zee, Tuin, Ster];

    public static string Normalize(string? key)
        => All.FirstOrDefault(t => string.Equals(t, key, StringComparison.OrdinalIgnoreCase)) ?? Zee;

    public static string CssClass(string? key) => "ll-theme-" + Normalize(key);

    /// <summary>Localization key for the themed stop title (logical <paramref name="worldKey"/>).</summary>
    public static string WorldTitleKey(string? themeKey, string worldKey)
    {
        themeKey = Normalize(themeKey);
        if (string.Equals(worldKey, "pauze-eiland", StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.World.Island.Title";
        }

        return (themeKey, worldKey.ToLowerInvariant()) switch
        {
            (Zee, "koraalrif") => "Leerling.World.Reef.Title",
            (Zee, "schatgrot") => "Leerling.World.Cave.Title",
            (Zee, "vuurtoren") => "Leerling.World.Lighthouse.Title",
            (Zee, "lagune") => "Leerling.World.Lagoon.Title",
            (Tuin, "koraalrif") => "Leerling.Theme.Tuin.Reef.Title",
            (Tuin, "schatgrot") => "Leerling.Theme.Tuin.Cave.Title",
            (Tuin, "vuurtoren") => "Leerling.Theme.Tuin.Lighthouse.Title",
            (Tuin, "lagune") => "Leerling.Theme.Tuin.Lagoon.Title",
            (Ster, "koraalrif") => "Leerling.Theme.Ster.Reef.Title",
            (Ster, "schatgrot") => "Leerling.Theme.Ster.Cave.Title",
            (Ster, "vuurtoren") => "Leerling.Theme.Ster.Lighthouse.Title",
            (Ster, "lagune") => "Leerling.Theme.Ster.Lagoon.Title",
            _ => PupilWorldCatalog.Find(worldKey)?.TitleKey ?? "Leerling.World.Reef.Title",
        };
    }

    public static string WorldSubtitleKey(string? themeKey, string worldKey)
    {
        themeKey = Normalize(themeKey);
        if (string.Equals(worldKey, "pauze-eiland", StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.World.Island.Sub";
        }

        return (themeKey, worldKey.ToLowerInvariant()) switch
        {
            (Zee, "koraalrif") => "Leerling.World.Reef.Sub",
            (Zee, "schatgrot") => "Leerling.World.Cave.Sub",
            (Zee, "vuurtoren") => "Leerling.World.Lighthouse.Sub",
            (Zee, "lagune") => "Leerling.World.Lagoon.Sub",
            (Tuin, _) => "Leerling.Theme.Tuin.World.Sub",
            (Ster, _) => "Leerling.Theme.Ster.World.Sub",
            _ => PupilWorldCatalog.Find(worldKey)?.SubtitleKey ?? "Leerling.World.Reef.Sub",
        };
    }

    public static string ThemeCardTitleKey(string themeKey) => Normalize(themeKey) switch
    {
        Tuin => "Leerling.Theme.Tuin.Name",
        Ster => "Leerling.Theme.Ster.Name",
        _ => "Leerling.Theme.Zee.Name",
    };

    public static string ThemeCardLeadKey(string themeKey) => Normalize(themeKey) switch
    {
        Tuin => "Leerling.Theme.Tuin.Lead",
        Ster => "Leerling.Theme.Ster.Lead",
        _ => "Leerling.Theme.Zee.Lead",
    };

    public static string ThemeHeroImage(string themeKey) => Normalize(themeKey) switch
    {
        Tuin => "/images/leerling/werelden/tuin-baai.jpg",
        Ster => "/images/leerling/werelden/sterren-kaart.jpg",
        _ => "/images/leerling/werelden/zee-haven.jpg",
    };

    public static bool RequiresThemeChoice(PupilQuestionSet set, string? visualThemeKey)
        => set == PupilQuestionSet.Groep78
           && string.IsNullOrWhiteSpace(visualThemeKey);

    /// <summary>Theme switch only before the first answer or while on Pauze-eiland.</summary>
    public static bool CanChangeVisualTheme(
        PupilQuestionSet set,
        PupilCodeStatus status,
        DateTime? completedAtUtc,
        IReadOnlyDictionary<string, int> answers,
        bool islandDone,
        PupilQuestionSetDef def,
        IReadOnlyDictionary<string, PupilPuzzleStatus>? puzzles)
    {
        if (set != PupilQuestionSet.Groep78)
        {
            return false;
        }

        if (status == PupilCodeStatus.Completed || completedAtUtc is not null)
        {
            return false;
        }

        if (answers.Count == 0)
        {
            return true;
        }

        var step = PupilFlow.Next(def, answers, islandDone, puzzles);
        return step.Kind == PupilFlowStepKind.Island;
    }

    public static string PuzzleTitleKey(string? themeKey, string puzzleKey)
    {
        var puzzle = puzzleKey.Trim().ToLowerInvariant();
        themeKey = Normalize(themeKey);
        return (themeKey, puzzle) switch
        {
            (Tuin, "schelpenrij") => "Leerling.Puzzle.Tuin.Schelpenrij.Title",
            (Tuin, "schatkaart") => "Leerling.Puzzle.Tuin.Schatkaart.Title",
            (Tuin, "vuurtorenlampen") => "Leerling.Puzzle.Tuin.Vuurtoren.Title",
            (Ster, "schelpenrij") => "Leerling.Puzzle.Ster.Schelpenrij.Title",
            (Ster, "schatkaart") => "Leerling.Puzzle.Ster.Schatkaart.Title",
            (Ster, "vuurtorenlampen") => "Leerling.Puzzle.Ster.Vuurtoren.Title",
            (_, "schelpenrij") => "Leerling.Puzzle.Schelpenrij.Title",
            (_, "schatkaart") => "Leerling.Puzzle.Schatkaart.Title",
            (_, "vuurtorenlampen") => "Leerling.Puzzle.Vuurtoren.Title",
            _ => "Leerling.Puzzle.Title",
        };
    }

    public static string PuzzleLeadKey(string? themeKey, string puzzleKey)
    {
        var puzzle = puzzleKey.Trim().ToLowerInvariant();
        themeKey = Normalize(themeKey);
        return (themeKey, puzzle) switch
        {
            (Tuin, "schelpenrij") => "Leerling.Puzzle.Tuin.Schelpenrij.Lead",
            (Tuin, "schatkaart") => "Leerling.Puzzle.Tuin.Schatkaart.Lead",
            (Tuin, "vuurtorenlampen") => "Leerling.Puzzle.Tuin.Vuurtoren.Lead",
            (Ster, "schelpenrij") => "Leerling.Puzzle.Ster.Schelpenrij.Lead",
            (Ster, "schatkaart") => "Leerling.Puzzle.Ster.Schatkaart.Lead",
            (Ster, "vuurtorenlampen") => "Leerling.Puzzle.Ster.Vuurtoren.Lead",
            (_, "schelpenrij") => "Leerling.Puzzle.Schelpenrij.Lead",
            (_, "schatkaart") => "Leerling.Puzzle.Schatkaart.Lead",
            (_, "vuurtorenlampen") => "Leerling.Puzzle.Vuurtoren.Lead",
            _ => "Leerling.Puzzle.Lead",
        };
    }

    /// <summary>Memory-match pair ids (same logic; art via <see cref="PuzzleMatchAsset"/>).</summary>
    public static IReadOnlyList<string> PuzzleMatchPairIds(string? themeKey)
    {
        _ = Normalize(themeKey);
        return ["1", "2", "3", "1", "2", "3"];
    }

    /// <summary>Sort puzzle piece for logical size 1–4 (schelpenrij / zaadrij / steenpad).</summary>
    public static string PuzzleSortAsset(string? themeKey, int size)
    {
        var theme = Normalize(themeKey);
        var clamped = Math.Clamp(size, 1, 4);
        var ext = theme == Zee ? "png" : "jpg";
        return $"/images/leerling/werelden/puzzles/{theme}/sort-{clamped}.{ext}";
    }

    /// <summary>Memory-match face art for pair id 1–3.</summary>
    public static string PuzzleMatchAsset(string? themeKey, string pairId)
    {
        var theme = Normalize(themeKey);
        var id = pairId.Trim();
        if (id is not ("1" or "2" or "3"))
        {
            id = "1";
        }

        return $"/images/leerling/werelden/puzzles/{theme}/match-{id}.jpg";
    }

    /// <summary>Vuurtorenlampen lamp art (0-based index).</summary>
    public static string PuzzleLampAsset(string? themeKey, int lampIndex)
    {
        var theme = Normalize(themeKey);
        var n = Math.Clamp(lampIndex + 1, 1, 3);
        return $"/images/leerling/werelden/puzzles/{theme}/lamp-{n}.jpg";
    }

    public static string PuzzleCardBackAsset(string? themeKey)
    {
        var theme = Normalize(themeKey);
        return $"/images/leerling/werelden/puzzles/{theme}/card-back.jpg";
    }

    /// <summary>All puzzle raster paths (relative to wwwroot) for cache busting.</summary>
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
