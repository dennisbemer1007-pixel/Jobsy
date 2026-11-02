using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Post-login and post-theme entry routes for groep 7/8.</summary>
public static class PupilGroep78EntryPaths
{
    public const string Wereld = "/leerling/wereld";
    public const string Start = "/leerling/start";
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

        if (!HasExplicitJourneyThemeChoice(progress))
        {
            return Wereld;
        }

        return status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0)
            ? Start
            : Reis;
    }

    public static string AfterLogin(PupilProgressStateDto progress, bool completed)
        => AfterLogin(
            progress.QuestionSet,
            progress.Status,
            MapProgress(progress),
            completed);

    private static PupilProgress MapProgress(PupilProgressStateDto progress)
        => new()
        {
            CurrentIndex = progress.CurrentIndex,
            JourneyThemeKey = progress.JourneyThemeKey,
            JourneyThemeChosenAtUtc = progress.JourneyThemeChosenAtUtc,
        };

    public static string AfterThemeChosen(PupilCodeStatus status, PupilProgress? progress)
        => status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0)
            ? Start
            : Reis;

    public static bool HasExplicitJourneyThemeChoice(PupilProgress? progress)
        => progress?.JourneyThemeChosenAtUtc is not null
           && PupilJourneyTheme.IsValid(progress.JourneyThemeKey);

    public static bool HasExplicitJourneyThemeChoice(PupilProgressStateDto progress)
        => HasExplicitJourneyThemeChoice(MapProgress(progress));

    public static bool RequiresThemeChoice(PupilQuestionSet questionSet, PupilProgress? progress)
        => questionSet == PupilQuestionSet.Groep78
           && !HasExplicitJourneyThemeChoice(progress);

    public static bool RequiresThemeChoice(PupilQuestionSet questionSet, string? journeyThemeKey, DateTime? journeyThemeChosenAtUtc)
        => questionSet == PupilQuestionSet.Groep78
           && !(journeyThemeChosenAtUtc is not null && PupilJourneyTheme.IsValid(journeyThemeKey));

    public static bool RequiresThemeChoice(PupilProgressStateDto progress)
        => RequiresThemeChoice(progress.QuestionSet, progress.JourneyThemeKey, progress.JourneyThemeChosenAtUtc);

    /// <summary>G78 journey steps that must not run without a chosen visual theme.</summary>
    public static bool IsThemeGatedPath(ReadOnlySpan<char> path)
    {
        if (path.Equals("/leerling/start", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/leerling/reis", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/leerling/vakken", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/leerling/eiland", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWith("/leerling/puzzel", StringComparison.OrdinalIgnoreCase);
    }
}
