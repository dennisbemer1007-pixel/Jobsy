using Jobsy.Core.Authorization;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class AssistantClientEndpoints
{
    public static IEndpointRouteBuilder MapAssistantClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assistant-client/chat", ChatAsync)
            .RequireAuthorization()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> ChatAsync(
        AssistantClientChatRequest body,
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IChatContextGreetingGate greetingGate,
        CancellationToken cancellationToken)
    {
        if (!AssistantChatHost.IsAvailableFor(http.User))
        {
            return Results.Forbid();
        }

        var messages = body.Messages ?? [];
        var warm = body.Warm ?? true;
        var pagePath = string.IsNullOrWhiteSpace(body.PagePath)
            ? http.Request.Path.Value
            : body.PagePath;

        if (messages.Count == 0)
        {
            if (warm && greetingGate.Enabled
                && RoleClaimMatching.HasRole(http.User, JobsyRoles.Candidate))
            {
                try
                {
                    var boot = await CandidateChatContextBuilder.BuildForPagePathAsync(
                        api,
                        pagePath ?? "/",
                        culture,
                        cancellationToken);
                    return Results.Json(new AssistantClientChatResponse
                    {
                        Reply = boot.Greeting,
                        PageContextLine = boot.PageContextLine,
                        Chips = boot.Chips.Select(c => new AssistantClientChip(c.Label, c.Message)).ToList()
                    });
                }
                catch
                {
                    // Fall through to static greeting (no upstream AI call on open).
                }
            }

            return Results.Json(new AssistantClientChatResponse
            {
                Reply = culture["Assistant.Greeting"]
            });
        }

        try
        {
            var history = AssistantChatHistory.ForRequest(messages).ToList();
            var reply = await api.SendAssistantChatAsync(history, cancellationToken);
            return Results.Json(new AssistantClientChatResponse
            {
                Reply = reply.Reply,
                Actions = reply.Actions
            });
        }
        catch
        {
            return Results.Json(new AssistantClientChatResponse
            {
                Reply = culture["Assistant.Greeting"]
            }, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private sealed class AssistantClientChatRequest
    {
        public IReadOnlyList<AssistantChatMessage>? Messages { get; init; }
        public string? PagePath { get; init; }
        public bool? Warm { get; init; }
    }

    private sealed class AssistantClientChatResponse
    {
        public string Reply { get; init; } = "";
        public string? PageContextLine { get; init; }
        public List<AssistantClientChip> Chips { get; init; } = [];
        public List<AssistantChatActionItem> Actions { get; init; } = [];
    }

    private sealed record AssistantClientChip(string Label, string Message);
}
