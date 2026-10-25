using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchEmployerOutcomeClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchEmployerOutcomeClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-employer-outcome-client/record-outcome", RecordOutcomeAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> RecordOutcomeAsync(
        Guid applicationId,
        RecordOutcomeBody body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty
            || body.Outcome is null
            || string.IsNullOrWhiteSpace(body.Reason))
        {
            return Results.BadRequest();
        }

        if (!Enum.TryParse<AfterMatchOutcomeKind>(body.Outcome, ignoreCase: true, out var outcome))
        {
            return Results.BadRequest();
        }

        try
        {
            var thread = await api.RecordAfterMatchOutcomeAsync(
                applicationId,
                new RecordOutcomeRequest(outcome, body.Reason.Trim()),
                cancellationToken);

            var redirect = thread.Outcome is not null
                ? $"/werkgever/sollicitaties/{applicationId:D}/after-match?panel=outcome"
                : $"/werkgever/sollicitaties/{applicationId:D}/after-match?panel=outcome";

            return Results.Json(new { redirectUrl = redirect });
        }
        catch
        {
            return Results.BadRequest();
        }
    }

    private sealed record RecordOutcomeBody(string? Outcome, string? Reason);
}
