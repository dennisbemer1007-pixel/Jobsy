using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Post-login and post-theme entry routes for groep 7/8.</summary>
public static class PupilGroep78EntryPaths
{
    public const string Wereld = "/leerling/wereld";
    public const string Start = "/leerling/start";
    public const string Route = "/leerling/route";
    public const string Reis = "/leerling/reis";
    public const string Result = "/leerling/dit-ben-jij";

    public static string AfterLogin(
        PupilQuestionSet questionSet,
        PupilCodeStatus status,
        PupilProgress? progress,
        bool completed)
    {
        if (questionSet != PupilQuestionSet.Groep78)
        {
            return completed
                ? Result
                : status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0)
                    ? Start
                    : Reis;
        }

        if (completed)
        {
            return Result;
        }

        if (!PupilJourneyTheme.IsValid(progress?.JourneyThemeKey))
        {
            return Wereld;
        }

        return AtJourneyStart(status, progress)
            ? RouteForTheme(progress?.JourneyThemeKey)
            : Reis;
    }

    public static string AfterThemeChosen(PupilCodeStatus status, PupilProgress? progress, string themeKey)
        => AtJourneyStart(status, progress)
            ? RouteForTheme(themeKey)
            : Reis;

    private static bool AtJourneyStart(PupilCodeStatus status, PupilProgress? progress)
        => status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0);

    private static string RouteForTheme(string? themeKey)
        => string.Equals(themeKey, PupilJourneyTheme.ZeeExpeditie, StringComparison.OrdinalIgnoreCase)
           || string.Equals(themeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase)
           || string.Equals(themeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase)
            ? Route
            : Start;
}
