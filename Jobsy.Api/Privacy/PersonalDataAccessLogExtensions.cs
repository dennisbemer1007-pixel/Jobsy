using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Privacy;

public static class PersonalDataAccessLogExtensions
{
    public static Task LogPersonalDataAccessAsync(
        this ControllerBase controller,
        IPersonalDataAccessLogger logger,
        Guid actorUserId,
        string actorRole,
        string resource,
        string action,
        Guid? subjectUserId = null,
        Guid? subjectCompanyId = null,
        string? reason = null,
        Guid? supportAccessGrantId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new PersonalDataAccessEntry(
            ActorUserId: actorUserId,
            ActorRole: actorRole,
            Resource: resource,
            Action: action,
            SubjectUserId: subjectUserId,
            SubjectCompanyId: subjectCompanyId,
            Reason: reason,
            SupportAccessGrantId: supportAccessGrantId,
            CorrelationId: controller.HttpContext.TraceIdentifier,
            IpAddress: controller.HttpContext.Connection.RemoteIpAddress?.ToString());
        return logger.LogAsync(entry, cancellationToken);
    }

    public static string ResolveActorRole(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role)
           ?? user.FindFirstValue("role")
           ?? "Unknown";

    public static bool IsMfaVerifiedInSession(ClaimsPrincipal user)
        => string.Equals(
            user.FindFirstValue(JobsyClaimTypes.MfaVerified),
            "1",
            StringComparison.Ordinal);
}
