using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Applications;
using Jobsy.Web.Localization;
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
        CultureState culture,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var parsed = AfterMatchProposeSlotParse.TryParse(body.DateLabel, body.TimeWindow);
        if (parsed.ErrorKey is not null)
        {
            return Results.Json(
                new { errorKey = parsed.ErrorKey, message = culture[parsed.ErrorKey] },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var note = string.IsNullOrWhiteSpace(body.Note) ? "Voorstel kandidaat" : body.Note.Trim();
        var reason = $"{body.DateLabel.Trim()}, {body.TimeWindow.Trim()}. {note}";

        try
        {
            await api.ProposeCandidateAfterMatchSlotAsync(
                applicationId,
                new ProposeCandidateSlotRequest(parsed.StartUtc, parsed.EndUtc, reason),
                cancellationToken);
        }
        catch
        {
            return Results.Json(
                new { errorKey = "Common.Error.TryAgain", message = culture["Common.Error.TryAgain"] },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var returnUrl = http.Request.Query["return"].ToString();
        var redirect = AfterMatchSollicitatiesUrls.AfterMatchBase(applicationId, returnUrl);
        return Results.Json(new { redirectUrl = redirect });
    }

    private sealed record SelectSlotBody(Guid SlotId);

    private sealed record ProposeSlotBody(string DateLabel, string TimeWindow, string Note);
}
