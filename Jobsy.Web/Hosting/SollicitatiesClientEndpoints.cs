using Jobsy.Core.Authorization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class SollicitatiesClientEndpoints
{
    public static IEndpointRouteBuilder MapSollicitatiesClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sollicitaties-client/berichten/mark-read", MarkBerichtenReadAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> MarkBerichtenReadAsync(
        Guid applicationId,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            return Results.BadRequest();
        }

        var ok = await api.TryMarkCandidateAfterMatchMessagesReadAsync(applicationId, cancellationToken);
        return ok ? Results.NoContent() : Results.NotFound();
    }
}
