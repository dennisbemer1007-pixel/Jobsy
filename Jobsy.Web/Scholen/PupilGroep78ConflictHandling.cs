using System.Net;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Scholen;

/// <summary>Maps pupil API 409 responses to UI state (avoid false "test closed" screens).</summary>
public static class PupilGroep78ConflictHandling
{
    public const string WindowClosedCode = "window_closed";

    public static bool IsWindowClosed(HttpRequestException ex)
        => ex.StatusCode == HttpStatusCode.Conflict
           && ex.Message.Contains(WindowClosedCode, StringComparison.OrdinalIgnoreCase);

    public static bool TryHandleProgressConflict(
        NavigationManager nav,
        PupilProgressStateDto? progress,
        HttpRequestException ex)
    {
        if (ex.StatusCode != HttpStatusCode.Conflict)
        {
            return false;
        }

        if (progress is not null && PupilGroep78Navigation.RedirectIfThemeMissing(nav, progress))
        {
            return true;
        }

        if (progress is not null
            && ex.Message.Contains("theme_required", StringComparison.OrdinalIgnoreCase)
            && PupilGroep78EntryPaths.RequiresThemeChoice(progress))
        {
            nav.NavigateTo(PupilGroep78EntryPaths.Wereld);
            return true;
        }

        return false;
    }
}
