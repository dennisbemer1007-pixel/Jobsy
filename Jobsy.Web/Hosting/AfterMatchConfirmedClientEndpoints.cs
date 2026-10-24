using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchConfirmedClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchConfirmedClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ndm-confirmed-client/calendar.ics", DownloadCalendarAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPost("/api/ndm-confirmed-client/reminders", UpdateRemindersAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPost("/api/ndm-confirmed-client/cancel", CancelInterviewAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> DownloadCalendarAsync(
        Guid applicationId,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var bytes = await api.TryDownloadAfterMatchCalendarAsync(applicationId, cancellationToken);
        return bytes is null
            ? Results.NotFound()
            : Results.File(bytes, "text/calendar", "lobsy-gesprek.ics");
    }

    private static async Task<IResult> UpdateRemindersAsync(
        Guid applicationId,
        UpdateReminderPreferenceRequest body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        _ = applicationId;
        try
        {
            var prefs = await api.UpdateAfterMatchReminderPreferencesAsync(body, cancellationToken);
            return Results.Json(prefs);
        }
        catch
        {
            return Results.BadRequest();
        }
    }

    private static async Task<IResult> CancelInterviewAsync(
        Guid applicationId,
        RescheduleRequest body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(body.Reason))
        {
            return Results.BadRequest();
        }

        try
        {
            var thread = await api.CancelAfterMatchInterviewAsync(applicationId, body, cancellationToken);
            return Results.Json(thread);
        }
        catch
        {
            return Results.BadRequest();
        }
    }
}
