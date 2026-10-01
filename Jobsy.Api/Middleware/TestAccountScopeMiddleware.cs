using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Ops;

namespace Jobsy.Api.Middleware;

/// <summary>Enters <see cref="TestAccountScope"/> when the principal is a test account.</summary>
public sealed class TestAccountScopeMiddleware
{
    private readonly RequestDelegate _next;

    public TestAccountScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (TestDataRules.IsTestViewer(context.User))
        {
            using (TestAccountScope.Enter())
            {
                await _next(context);
            }

            return;
        }

        await _next(context);
    }
}
