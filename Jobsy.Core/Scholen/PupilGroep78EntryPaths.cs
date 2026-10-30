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

        if (!PupilJourneyTheme.IsValid(progress?.JourneyThemeKey))
        {
            return Wereld;
        }

        return status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0)
            ? Start
            : Reis;
    }

    public static string AfterThemeChosen(PupilCodeStatus status, PupilProgress? progress)
        => status == PupilCodeStatus.NotStarted && (progress is null || progress.CurrentIndex == 0)
            ? Start
            : Reis;
}
