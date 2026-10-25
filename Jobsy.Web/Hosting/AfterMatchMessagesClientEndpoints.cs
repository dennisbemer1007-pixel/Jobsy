using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AfterMatchMessagesClientEndpoints
{
    public static IEndpointRouteBuilder MapAfterMatchMessagesClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/applications/{applicationId:guid}/after-match/messages/preview", PreviewAsync)
            .RequireAuthorization()
            .DisableAntiforgery();

        app.MapPost("/api/applications/{applicationId:guid}/after-match/messages", SendAsync)
            .RequireAuthorization()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> PreviewAsync(
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
        return preview is null ? Results.NotFound() : Results.Json(preview);
    }

    private static async Task<IResult> SendAsync(
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
