using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

public static class PupilZeePuzzleNavigation
{
    public static string? TargetPath(
        PupilQuestionSet questionSet,
        string? journeyThemeKey,
        string? nextStep,
        string? puzzleKey)
    {
        if (!string.Equals(nextStep, "puzzle", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(puzzleKey)
            || !PupilPuzzleRoutes.IsBuilt(puzzleKey))
        {
            return null;
        }

        if (questionSet == PupilQuestionSet.Groep78
            && string.Equals(journeyThemeKey, PupilJourneyTheme.ZeeExpeditie, StringComparison.OrdinalIgnoreCase)
            && PupilPuzzleIntroRoutes.IsBuilt(puzzleKey))
        {
            return PupilPuzzleIntroRoutes.Path(puzzleKey);
        }

        return $"/leerling/puzzel/{Uri.EscapeDataString(puzzleKey)}";
    }
}
