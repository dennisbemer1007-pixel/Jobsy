using System.Reflection;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jobsy.Api.Filters;

/// <summary>
/// Global filter: returns 404 ProblemDetails (type=feature_disabled) when a
/// <see cref="RequiresFeatureAttribute"/> requirement is not met.
/// Admins bypass class-level gates; method-level attributes still apply
/// (e.g. ATS scrape triggers). Controllers named Admin* without method attrs are skipped.
/// </summary>
public sealed class FeatureGateFilter : IAsyncActionFilter
{
    public const string FeatureDisabledType = "feature_disabled";

    private readonly IFeatureFlags _flags;

    public FeatureGateFilter(IFeatureFlags flags)
    {
        _flags = flags;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var methodAttrs = Array.Empty<RequiresFeatureAttribute>();
        if (context.ActionDescriptor is ControllerActionDescriptor cad)
        {
            methodAttrs = cad.MethodInfo
                .GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
                .OfType<RequiresFeatureAttribute>()
                .ToArray();
        }

        var isAdminUser = RoleClaimMatching.HasRole(context.HttpContext.User, JobsyRoles.Admin);

        // Method-level gates always apply (even for admins / Admin* controllers).
        // Class-level gates are skipped for admins and for admin-only controllers.
        List<RequiresFeatureAttribute> attributes;
        if (methodAttrs.Length > 0)
        {
            attributes = methodAttrs.ToList();
        }
        else if (isAdminUser
                 || (context.ActionDescriptor is ControllerActionDescriptor cad2
                     && IsAdminOnlyController(cad2.ControllerTypeInfo)))
        {
            await next();
            return;
        }
        else
        {
            attributes = context.ActionDescriptor.EndpointMetadata
                .OfType<RequiresFeatureAttribute>()
                .ToList();
        }

        if (attributes.Count == 0)
        {
            await next();
            return;
        }

        var snap = await _flags.GetAsync(context.HttpContext.RequestAborted);
        foreach (var attr in attributes)
        {
            var enabled = snap.IsEnabled(attr.Feature);
            var ok = attr.WhenEnabled ? enabled : !enabled;
            if (!ok)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = $"Feature '{attr.Feature}' is not available.",
                    Type = FeatureDisabledType
                })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                return;
            }
        }

        await next();
    }

    private static bool IsAdminOnlyController(TypeInfo controllerType)
    {
        var ns = controllerType.Namespace ?? "";
        if (ns.Contains(".Admin", StringComparison.Ordinal)
            || controllerType.Name.StartsWith("Admin", StringComparison.Ordinal))
        {
            return true;
        }

        var authorize = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .ToList();
        if (authorize.Count == 0)
        {
            return false;
        }

        return authorize.All(a =>
            !string.IsNullOrWhiteSpace(a.Roles)
            && a.Roles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .All(r => string.Equals(r, JobsyRoles.Admin, StringComparison.OrdinalIgnoreCase)));
    }
}
