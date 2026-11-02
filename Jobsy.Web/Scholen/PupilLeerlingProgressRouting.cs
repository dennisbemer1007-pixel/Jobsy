using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Web.Scholen;

/// <summary>Server-side redirect targets after loading pupil progress (mirrors Blazor pages).</summary>
public static class PupilLeerlingProgressRouting
{
    public static string? RedirectForStart(PupilProgressStateDto progress)
    {
        if (progress.QuestionSet == PupilQuestionSet.MboHbo)
        {
            return PupilGroep78Navigation.MboHboStartRedirect(progress);
        }

        return PupilGroep78Navigation.ThemeRedirectPath(progress);
    }

    public static string? RedirectForWereld(PupilProgressStateDto progress)
    {
        if (progress.QuestionSet != PupilQuestionSet.Groep78)
        {
            return progress.Completed ? PupilGroep78EntryPaths.Result : PupilGroep78EntryPaths.Start;
        }

        if (progress.Completed)
        {
            return PupilGroep78EntryPaths.Result;
        }

        return null;
    }

    public static string? RedirectForReis(PupilProgressStateDto state)
    {
        if (state.Completed || string.Equals(state.NextStep, "done", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(state.NextStep, "vakken", StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/vakken";
        }

        if (state.NeedsIsland || string.Equals(state.NextStep, "island", StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/eiland";
        }

        if (string.Equals(state.NextStep, "puzzle", StringComparison.OrdinalIgnoreCase)
            && PupilPuzzleRoutes.IsBuilt(state.NextPuzzleKey))
        {
            return $"/leerling/puzzel/{Uri.EscapeDataString(state.NextPuzzleKey!)}";
        }

        return null;
    }

    public static string? RedirectAfterAnswer(PupilAnswerResponse result)
    {
        if (result.Completed || string.Equals(result.NextStep, "done", StringComparison.OrdinalIgnoreCase))
        {
            return PupilGroep78EntryPaths.Result;
        }

        if (string.Equals(result.NextStep, "vakken", StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/vakken";
        }

        if (result.NeedsIsland || string.Equals(result.NextStep, "island", StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/eiland";
        }

        if (string.Equals(result.NextStep, "puzzle", StringComparison.OrdinalIgnoreCase)
            && PupilPuzzleRoutes.IsBuilt(result.NextPuzzleKey))
        {
            return $"/leerling/puzzel/{Uri.EscapeDataString(result.NextPuzzleKey!)}";
        }

        return null;
    }

    public static string? RedirectForEiland(PupilProgressStateDto state)
    {
        if (state.Completed)
        {
            return PupilGroep78EntryPaths.Result;
        }

        var atIsland = state.NeedsIsland
                       || string.Equals(state.NextStep, "island", StringComparison.OrdinalIgnoreCase);
        return atIsland ? null : "/leerling/reis";
    }

    public static string? RedirectForVakken(PupilProgressStateDto state)
    {
        if (state.Completed || string.Equals(state.NextStep, "done", StringComparison.OrdinalIgnoreCase))
        {
            return PupilGroep78EntryPaths.Result;
        }

        if (string.Equals(state.NextStep, "vakken", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return "/leerling/reis";
    }

    public static string? RedirectForPuzzle(PupilProgressStateDto state, string puzzleKey)
    {
        if (!string.Equals(state.NextStep, "puzzle", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(state.NextPuzzleKey, puzzleKey, StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/reis";
        }

        if (state.QuestionSet != PupilQuestionSet.Groep78)
        {
            return "/leerling/reis";
        }

        return null;
    }

    public static string? RedirectAfterPuzzle(PupilPuzzleResponse result)
    {
        if (result.NeedsIsland || string.Equals(result.NextStep, "island", StringComparison.OrdinalIgnoreCase))
        {
            return "/leerling/eiland";
        }

        if (string.Equals(result.NextStep, "puzzle", StringComparison.OrdinalIgnoreCase)
            && PupilPuzzleRoutes.IsBuilt(result.NextPuzzleKey))
        {
            return $"/leerling/puzzel/{Uri.EscapeDataString(result.NextPuzzleKey!)}";
        }

        return "/leerling/reis";
    }

    public static string? RedirectAfterChips(PupilChipsResponse _, PupilQuestionSetDef? def, bool partBreakShown)
    {
        if (def?.PartBreakAfterIsland == true && !partBreakShown)
        {
            return null;
        }

        return "/leerling/reis";
    }
}
