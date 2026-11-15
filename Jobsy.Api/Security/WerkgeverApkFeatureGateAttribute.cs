using Jobsy.Core.Features;
using Jobsy.Core.WerkgeverApk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jobsy.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class WerkgeverApkFeatureGateAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var features = context.HttpContext.RequestServices.GetService(typeof(IFeatureFlags)) as IFeatureFlags;
        if (features is null)
        {
            context.Result = new NotFoundObjectResult(new { error = "feature_disabled" });
            return;
        }

        if (!await WerkgeverApkFeatureGate.IsGlobalEnabledAsync(features, context.HttpContext.RequestAborted))
        {
            context.Result = new NotFoundObjectResult(new { error = "feature_disabled" });
            return;
        }

        await next();
    }
}