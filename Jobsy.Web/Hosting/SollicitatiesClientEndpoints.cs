using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class SollicitatiesClientEndpoints
{
    public static IEndpointRouteBuilder MapSollicitatiesClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sollicitaties-client/activiteiten", AddActiviteitAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPut("/api/sollicitaties-client/activiteiten/agreement", UpdateAgreementAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapGet("/api/sollicitaties-client/activiteiten/period.pdf", DownloadActiviteitenPdfAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate });

        app.MapGet("/api/sollicitaties-client/activiteiten/{activityId:guid}/attachment", DownloadActiviteitAttachmentAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate });

        app.MapPost("/api/sollicitaties-client/berichten/mark-read", MarkBerichtenReadAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        // Stale cached sollicitaties-berichten.js may still POST here; same handler as the BFF above.
        app.MapPost("/api/me/applications/{applicationId:guid}/after-match/messages/read", MarkBerichtenReadAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> AddActiviteitAsync(
        HttpRequest request,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await request.ReadFormAsync(cancellationToken);
        using var content = new MultipartFormDataContent();
        foreach (var field in form)
        {
            content.Add(new StringContent(field.Value.ToString()), field.Key);
        }

        foreach (var file in form.Files)
        {
            var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            }

            content.Add(streamContent, file.Name, file.FileName);
        }

        var ok = await api.TryAddManualJobSearchActivityAsync(content, cancellationToken);
        return ok ? Results.NoContent() : Results.BadRequest();
    }

    private static async Task<IResult> DownloadActiviteitAttachmentAsync(
        Guid activityId,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var file = await api.TryDownloadJobSearchActivityAttachmentAsync(activityId, cancellationToken);
        return file is null
            ? Results.NotFound()
            : Results.File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    private static async Task<IResult> UpdateAgreementAsync(
        UpdateActivityAgreementRequest request,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var ok = await api.TryUpdateJobSearchActivityAgreementAsync(request.ActivitiesPerPeriod, cancellationToken);
        return ok ? Results.NoContent() : Results.BadRequest();
    }

    private static async Task<IResult> DownloadActiviteitenPdfAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var bytes = await api.TryDownloadJobSearchActivitiesPdfAsync(cancellationToken);
        return bytes is null
            ? Results.NotFound()
            : Results.File(bytes, "application/pdf", "lobsy-sollicitatieactiviteiten.pdf");
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
