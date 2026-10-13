using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Auth;

/// <summary>
/// Gates <c>/banenkaart</c> when employers are OFF (Dependencies A absent: local <see cref="EmployersGate"/>).
/// </summary>
public sealed class BanenkaartGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && IsBanenkaartPath(context.Request.Path))
        {
            var employers = context.RequestServices.GetService<IEmployersSwitch>();
            if (employers is not null
                && !await EmployersGate.AllowOrRedirectAsync(
                    context,
                    employers,
                    FeatureRoutes.CandidateEmployersComingSoonPath,
                    context.RequestAborted))
            {
                return;
            }
        }

        await next(context);
    }

    private static bool IsBanenkaartPath(PathString path)
        => path.HasValue
           && (string.Equals(path.Value, PublicRoutes.Banenkaart, StringComparison.OrdinalIgnoreCase)
               || string.Equals(path.Value, PublicRoutes.Banenkaart + "/", StringComparison.OrdinalIgnoreCase));
}

public static class BanenkaartGateExtensions
{
    public static IApplicationBuilder UseBanenkaartGate(this IApplicationBuilder app)
        => app.UseMiddleware<BanenkaartGateMiddleware>();
}
