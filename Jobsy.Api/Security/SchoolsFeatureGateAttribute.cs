using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jobsy.Api.Security;

/// <summary>
/// Interim feature gate while RequiresFeatureAttribute is absent (Dependencies C).
/// Returns 404 feature_disabled when SchoolsEnabled is false.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SchoolsFeatureGateAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var features = context.HttpContext.RequestServices.GetService(typeof(IPlatformFeatureService))
            as IPlatformFeatureService;
        if (features is null)
        {
            await next();
            return;
        }

        if (!await SchoolsFeatureGate.IsEnabledAsync(features, context.HttpContext.RequestAborted))
        {
            context.Result = new NotFoundObjectResult(new { error = "feature_disabled" });
            return;
        }

        await next();
    }
}
