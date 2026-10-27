using Jobsy.Core.Authorization;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

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
        ILogger<AssistantClientEndpointsMarker> logger,
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
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Candidate chat context bootstrap failed for {PagePath}; using static greeting.",
                        pagePath);
                }
            }

            if (warm)
            {
                var employerBoot = await TryWarmEmployerAssistantBootAsync(
                    api,
                    http.User,
                    pagePath,
                    cancellationToken);
                if (employerBoot is not null)
                {
                    return Results.Json(employerBoot);
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
            var (resolvedPath, pageContextLine) = await ResolvePageContextForAssistantAsync(
                api,
                culture,
                greetingGate,
                http.User,
                pagePath,
                logger,
                cancellationToken);
            var reply = await api.SendAssistantChatAsync(
                history,
                resolvedPath,
                pageContextLine,
                cancellationToken);
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

    private sealed class AssistantClientEndpointsMarker;

    private static async Task<AssistantClientChatResponse?> TryWarmEmployerAssistantBootAsync(
        JobsyApiClient api,
        System.Security.Claims.ClaimsPrincipal user,
        string? pagePath,
        CancellationToken cancellationToken)
    {
        if (!AssistantChatHost.IsAvailableFor(user)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return null;
        }

        try
        {
            var reply = await api.SendAssistantChatAsync([], pagePath ?? "/", null, cancellationToken);
            return new AssistantClientChatResponse
            {
                Reply = reply.Reply,
                Actions = reply.Actions ?? []
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(string? PagePath, string? PageContextLine)> ResolvePageContextForAssistantAsync(
        JobsyApiClient api,
        CultureState culture,
        IChatContextGreetingGate greetingGate,
        System.Security.Claims.ClaimsPrincipal user,
        string? pagePath,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!greetingGate.Enabled || !RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return (pagePath, null);
        }

        try
        {
            var boot = await CandidateChatContextBuilder.BuildForPagePathAsync(
                api,
                pagePath ?? "/",
                culture,
                cancellationToken);
            return (pagePath, boot.PageContextLine);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Candidate chat context bootstrap failed for {PagePath}; continuing without page context line.",
                pagePath);
            return (pagePath, null);
        }
    }
}
