using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Scholen;

public static class PupilGroep78Navigation
{
    public static bool RedirectIfThemeMissing(NavigationManager nav, PupilProgressStateDto? progress)
    {
        if (progress is not null
            && PupilGroep78EntryPaths.RequiresThemeChoice(progress.QuestionSet, progress.JourneyThemeKey))
        {
            nav.NavigateTo(PupilGroep78EntryPaths.Wereld, replace: true);
            return true;
        }

        return false;
    }
}
