using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Applications;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchEmployerRescheduleClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchEmployerRescheduleClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-employer-reschedule-client/accept-slot", AcceptSlotAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> AcceptSlotAsync(
        Guid applicationId,
        AcceptSlotBody body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty || body.SlotId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        AfterMatchThreadDto thread;
        try
        {
            thread = await api.SelectAfterMatchSlotAsync(
                applicationId,
                new SelectSlotRequest(body.SlotId),
                cancellationToken);
        }
        catch
        {
            return Results.BadRequest();
        }

        var redirect = thread.InterviewStatus == InterviewStatus.Scheduled
            ? $"/werkgever/sollicitaties/{applicationId:D}/after-match"
            : $"/werkgever/sollicitaties/{applicationId:D}/after-match/reschedule-ssr";

        return Results.Json(new { redirectUrl = redirect });
    }

    private sealed record AcceptSlotBody(Guid SlotId);
}
