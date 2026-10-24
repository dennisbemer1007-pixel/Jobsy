using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchRescheduleClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchRescheduleClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-reschedule-client/reschedule", RescheduleAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPost("/api/ndm-reschedule-client/cancel", CancelAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> RescheduleAsync(
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
            var thread = await api.RescheduleAfterMatchAsync(applicationId, body, cancellationToken);
            return Results.Json(thread);
        }
        catch
        {
            return Results.BadRequest();
        }
    }

    private static async Task<IResult> CancelAsync(
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
