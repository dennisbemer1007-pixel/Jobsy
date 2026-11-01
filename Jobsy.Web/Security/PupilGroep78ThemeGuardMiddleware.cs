using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Security;

/// <summary>
/// Server-side redirect for groep 7/8 pupils who open journey steps without a
/// <see cref="Jobsy.Core.Entities.Scholen.PupilProgress.JourneyThemeKey"/>.
/// </summary>
public sealed class PupilGroep78ThemeGuardMiddleware
{
    private readonly RequestDelegate _next;

    public PupilGroep78ThemeGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, JobsyApiClient api)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "";
            if (PupilGroep78EntryPaths.IsThemeGatedPath(path)
                && context.User.Identity?.IsAuthenticated == true
                && JobsyApiAuthHandler.IsPupilPrincipal(context.User))
            {
                var progress = await api.GetPupilProgressAsync(context.RequestAborted);
                if (progress is not null
                    && PupilGroep78EntryPaths.RequiresThemeChoice(progress.QuestionSet, progress.JourneyThemeKey))
                {
                    context.Response.Redirect(PupilGroep78EntryPaths.Wereld);
                    return;
                }
            }
        }

        await _next(context);
    }
}
