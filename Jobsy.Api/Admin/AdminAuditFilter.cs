using System.Reflection;
using System.Security.Claims;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jobsy.Api.Admin;

/// <summary>
/// After an Admin action marked with <see cref="AdminAuditAttribute"/>, writes one audit event.
/// Result is derived from the HTTP status code (2xx success, 401/403 denied, else failed).
/// </summary>
public sealed class AdminAuditFilter : IAsyncActionFilter
{
    private readonly IAdminAuditLog _audit;
    private readonly IAdminAuditContext _context;
    private readonly IUserLookupService _users;

    public AdminAuditFilter(
        IAdminAuditLog audit,
        IAdminAuditContext context,
        IUserLookupService users)
    {
        _audit = audit;
        _context = context;
        _users = users;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (_context.SuppressAutoWrite)
        {
            return;
        }

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            return;
        }

        var attr = GetAuditAttribute(context);
        if (attr is null)
        {
            return;
        }

        var isAdmin = context.HttpContext.User.IsInRole(JobsyRoles.Admin);
        if (!isAdmin && !_context.ForceWrite)
        {
            return;
        }

        var statusCode = ResolveStatusCode(executed);
        var result = _context.ResultOverride
                     ?? statusCode switch
                     {
                         >= 200 and < 300 => AdminAuditKeys.Results.Success,
                         401 or 403 => AdminAuditKeys.Results.Denied,
                         _ => AdminAuditKeys.Results.Failed
                     };

        Guid? actorId = null;
        string actorRole = JobsyRoles.Admin;
        if (isAdmin || _context.ForceWrite)
        {
            try
            {
                var actor = await _users.FindByPrincipalAsync(
                    context.HttpContext.User,
                    context.HttpContext.RequestAborted);
                actorId = actor?.Id;
                actorRole = actor?.Role.ToString()
                            ?? context.HttpContext.User.FindFirstValue(ClaimTypes.Role)
                            ?? JobsyRoles.Admin;
            }
            catch
            {
                // Audit must not break the response.
            }
        }

        var targetId = _context.TargetId
                       ?? ResolveRouteValue(context, attr.TargetRouteKey);
        var targetType = string.IsNullOrWhiteSpace(_context.TargetType)
            ? attr.TargetType
            : _context.TargetType!;
        var action = string.IsNullOrWhiteSpace(_context.ActionOverride)
            ? attr.Action
            : _context.ActionOverride!;
        var correlation = context.HttpContext.TraceIdentifier;
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        await _audit.WriteAsync(
            new AdminAuditEntry(
                Action: action,
                TargetType: targetType,
                TargetId: targetId,
                TargetLabel: _context.TargetLabel,
                Reason: _context.Reason,
                DetailsJson: _context.DetailsJson,
                Result: result,
                ActorUserId: actorId,
                ActorRole: actorRole,
                ActorKind: isAdmin ? AdminAuditKeys.ActorKinds.Admin : AdminAuditKeys.ActorKinds.Self,
                CorrelationId: correlation,
                IpAddress: ip),
            context.HttpContext.RequestAborted);
    }

    private static AdminAuditAttribute? GetAuditAttribute(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor cad)
        {
            return null;
        }

        return cad.MethodInfo.GetCustomAttribute<AdminAuditAttribute>(inherit: true);
    }

    private static string? ResolveRouteValue(ActionExecutingContext context, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (context.RouteData.Values.TryGetValue(key, out var routeVal) && routeVal is not null)
        {
            return routeVal.ToString();
        }

        if (context.ActionArguments.TryGetValue(key, out var arg) && arg is not null)
        {
            return arg.ToString();
        }

        return null;
    }

    private static int ResolveStatusCode(ActionExecutedContext executed)
    {
        if (executed.Result is ObjectResult obj && obj.StatusCode is int code)
        {
            return code;
        }

        if (executed.Result is StatusCodeResult status)
        {
            return status.StatusCode;
        }

        if (executed.Result is EmptyResult or NoContentResult)
        {
            return StatusCodes.Status204NoContent;
        }

        if (executed.Result is OkObjectResult or OkResult)
        {
            return StatusCodes.Status200OK;
        }

        if (executed.Result is UnauthorizedResult)
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (executed.Result is ForbidResult)
        {
            return StatusCodes.Status403Forbidden;
        }

        if (executed.Result is NotFoundResult or NotFoundObjectResult)
        {
            return StatusCodes.Status404NotFound;
        }

        if (executed.Result is BadRequestResult or BadRequestObjectResult)
        {
            return StatusCodes.Status400BadRequest;
        }

        if (executed.Result is FileResult)
        {
            return StatusCodes.Status200OK;
        }

        return StatusCodes.Status200OK;
    }
}
