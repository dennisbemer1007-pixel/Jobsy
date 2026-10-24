using System.Globalization;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Applications;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchPickSlotClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchPickSlotClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-pick-slot-client/select-slot", SelectSlotAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPost("/api/ndm-pick-slot-client/propose-slot", ProposeSlotAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> SelectSlotAsync(
        Guid applicationId,
        SelectSlotBody body,
        JobsyApiClient api,
        HttpContext http,
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

        var returnUrl = http.Request.Query["return"].ToString();
        var redirect = thread.InterviewStatus == Core.Enums.InterviewStatus.Scheduled
            ? AfterMatchSollicitatiesUrls.AfterMatchBase(applicationId, returnUrl)
            : AfterMatchSollicitatiesUrls.PickSlot(applicationId, returnUrl);

        return Results.Json(new { redirectUrl = redirect });
    }

    private static async Task<IResult> ProposeSlotAsync(
        Guid applicationId,
        ProposeSlotBody body,
        JobsyApiClient api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty
            || string.IsNullOrWhiteSpace(body.StartLocal)
            || string.IsNullOrWhiteSpace(body.EndLocal)
            || string.IsNullOrWhiteSpace(body.Reason))
        {
            return Results.BadRequest();
        }

        if (!DateTime.TryParse(body.StartLocal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startLocal)
            || !DateTime.TryParse(body.EndLocal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endLocal))
        {
            return Results.BadRequest();
        }

        var startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

        try
        {
            await api.ProposeCandidateAfterMatchSlotAsync(
                applicationId,
                new ProposeCandidateSlotRequest(startUtc, endUtc, body.Reason.Trim()),
                cancellationToken);
        }
        catch
        {
            return Results.BadRequest();
        }

        var returnUrl = http.Request.Query["return"].ToString();
        var redirect = AfterMatchSollicitatiesUrls.AfterMatchBase(applicationId, returnUrl);
        return Results.Json(new { redirectUrl = redirect });
    }

    private sealed record SelectSlotBody(Guid SlotId);

    private sealed record ProposeSlotBody(string StartLocal, string EndLocal, string Reason);
}
