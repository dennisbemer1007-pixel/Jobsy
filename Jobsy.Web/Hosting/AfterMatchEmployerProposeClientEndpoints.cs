using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchEmployerProposeClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchEmployerProposeClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ndm-employer-propose-client/slots", ProposeSlotsAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.EmployerMutateRolesWithAdmin })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> ProposeSlotsAsync(
        Guid applicationId,
        ProposeBody body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty
            || body.Slots is null
            || body.Slots.Count is < 2 or > 3)
        {
            return Results.BadRequest();
        }

        var slots = body.Slots
            .Select(s => new ProposedSlotRequest(s.StartUtc, s.EndUtc))
            .ToList();

        var format = Enum.IsDefined(typeof(InterviewFormat), body.Format)
            ? (InterviewFormat)body.Format
            : InterviewFormat.OnSite;

        try
        {
            await api.ProposeEmployerAfterMatchSlotsAsync(
                applicationId,
                new ProposeEmployerSlotsRequest(
                    format,
                    body.LocationOrLink,
                    body.WithWhom,
                    body.Note,
                    slots),
                cancellationToken);
        }
        catch
        {
            return Results.BadRequest();
        }

        var redirect = $"/werkgever/sollicitaties/{applicationId:D}/after-match";
        return Results.Json(new { redirectUrl = redirect });
    }

    private sealed record ProposeBody(
        int Format,
        string? LocationOrLink,
        string? WithWhom,
        string? Note,
        List<SlotBody> Slots);

    private sealed record SlotBody(DateTime StartUtc, DateTime EndUtc);
}
