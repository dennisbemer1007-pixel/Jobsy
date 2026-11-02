using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Scholen;

public static class PupilGroep78Navigation
{
    public static bool RedirectIfThemeMissing(NavigationManager nav, PupilProgressStateDto? progress)
    {
        var path = ThemeRedirectPath(progress);
        if (path is null)
        {
            return false;
        }

        nav.NavigateTo(path, replace: true);
        return true;
    }

    /// <summary>SSR equivalent of <see cref="RedirectIfThemeMissing"/> — returns redirect URL or null.</summary>
    public static string? ThemeRedirectPath(PupilProgressStateDto? progress)
    {
        if (progress is not null
            && PupilGroep78EntryPaths.RequiresThemeChoice(progress.QuestionSet, progress.JourneyThemeKey))
        {
            return PupilGroep78EntryPaths.Wereld;
        }

        return null;
    }

    public static string? MboHboStartRedirect(PupilProgressStateDto? progress)
    {
        if (progress?.QuestionSet != PupilQuestionSet.MboHbo)
        {
            return null;
        }

        return progress.Completed
            ? PupilMboHboEntryPaths.Overzicht
            : PupilMboHboEntryPaths.Uitleg;
    }
}
