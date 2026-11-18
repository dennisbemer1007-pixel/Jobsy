using Jobsy.Core.Authorization;
using Jobsy.Core.Privacy;
using Jobsy.Web.Services;

namespace Jobsy.Web.Auth;

/// <summary>Fail-closed redirect for candidates without a stored date of birth on main-path GET/HEAD.</summary>
public sealed class CandidateDateOfBirthGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldEvaluate(context)
            && RoleClaimMatching.HasRole(context.User, JobsyRoles.Candidate))
        {
            var api = context.RequestServices.GetRequiredService<JobsyApiClient>();
            var profile = await api.GetMyProfileAsync(context.RequestAborted);
            var path = context.Request.Path.Value;
            var tab = context.Request.Query["tab"].ToString();
            if (profile is { HasDateOfBirth: false }
                && CandidateDateOfBirthPathRules.ShouldRedirect(path, tab, hasDateOfBirth: false))
            {
                var returnUrl = Uri.EscapeDataString((path ?? "/") + context.Request.QueryString);
                context.Response.Redirect($"{CandidateDateOfBirthPathRules.DobCapturePath}?returnUrl={returnUrl}");
                return;
            }
        }

        await next(context);
    }

    private static bool ShouldEvaluate(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        return context.User?.Identity?.IsAuthenticated == true;
    }
}

public static class CandidateDateOfBirthGateExtensions
{
    public static IApplicationBuilder UseCandidateDateOfBirthGate(this IApplicationBuilder app)
        => app.UseMiddleware<CandidateDateOfBirthGateMiddleware>();
}
