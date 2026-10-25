using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class WerkgeverClientEndpoints
{
    public static IEndpointRouteBuilder MapWerkgeverClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/werkgever-client/berichten/mark-read", MarkBerichtenReadAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
            .DisableAntiforgery();

        app.MapPost("/api/werkgever-client/berichten/preview", PreviewMessageAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
            .DisableAntiforgery();

        app.MapPost("/api/werkgever-client/berichten/send", SendMessageAsync)
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = $"{JobsyRoles.BranchManager},{JobsyRoles.RegionalManager},{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary},{JobsyRoles.Admin}"
            })
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

        var ok = await api.TryMarkEmployerAfterMatchMessagesReadAsync(applicationId, cancellationToken);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> PreviewMessageAsync(
        Guid applicationId,
        SendMessageRequest body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(body.Body))
        {
            return Results.BadRequest();
        }

        var preview = await api.PreviewAfterMatchMessageAsync(applicationId, body, cancellationToken);
        return preview is null ? Results.BadRequest() : Results.Json(preview);
    }

    private static async Task<IResult> SendMessageAsync(
        Guid applicationId,
        SendMessageRequest body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(body.Body))
        {
            return Results.BadRequest();
        }

        try
        {
            var thread = await api.SendAfterMatchMessageAsync(applicationId, body, cancellationToken);
            return Results.Json(thread);
        }
        catch
        {
            return Results.BadRequest();
        }
    }
}
