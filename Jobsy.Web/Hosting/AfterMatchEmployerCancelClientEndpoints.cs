using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchEmployerCancelClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchEmployerCancelClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-employer-cancel-client/cancel", CancelAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
            .DisableAntiforgery();

        return app;
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
            await api.CancelAfterMatchInterviewAsync(applicationId, body, cancellationToken);
        }
        catch
        {
            return Results.BadRequest();
        }

        var redirect =
            $"/werkgever/sollicitaties/{applicationId:D}/after-match?panel=propose";
        return Results.Json(new { redirectUrl = redirect });
    }
}
