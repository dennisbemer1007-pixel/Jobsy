using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class UwvClientEndpoints
{
    public static IEndpointRouteBuilder MapUwvClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/uwv-client/ondernemer", SaveOndernemerAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        app.MapPost("/api/uwv-client/adviseur-share", CreateAdviseurShareAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> SaveOndernemerAsync(HttpRequest request, JobsyApiClient api, CancellationToken cancellationToken)
    {
        SaveOndernemerBody? body;
        try
        {
            body = await request.ReadFromJsonAsync<SaveOndernemerBody>(cancellationToken);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        if (body?.Answers is null || body.Answers.Length != 6)
        {
            return Results.BadRequest();
        }

        try
        {
            var saved = await api.SaveUwvOndernemerAsync(body.Answers, cancellationToken);
            return saved is null ? Results.Problem(statusCode: StatusCodes.Status502BadGateway) : Results.Json(saved);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreateAdviseurShareAsync(HttpRequest request, JobsyApiClient api, CancellationToken cancellationToken)
    {
        CreateShareBody? body;
        try
        {
            body = await request.ReadFromJsonAsync<CreateShareBody>(cancellationToken);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        if (body?.Sections is null || body.Sections.Length == 0)
        {
            return Results.BadRequest();
        }

        var lang = string.Equals(body.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "nl";
        try
        {
            var created = await api.CreateUwvAdviseurShareAsync(body.Sections, lang, cancellationToken);
            return created is null ? Results.Problem(statusCode: StatusCodes.Status502BadGateway) : Results.Json(created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private sealed record SaveOndernemerBody(int[] Answers);

    private sealed record CreateShareBody(string[] Sections, string? Language);
}
