using Jobsy.Core.Features;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AmbassadorsFeatureMiddlewareTests
{
    [Fact]
    public async Task Sales_admin_is_blocked_when_ambassadors_are_off_and_when_flags_are_missing()
    {
        var blocked = new AmbassadorsFeatureMiddleware(_ => throw new InvalidOperationException("opened"));

        var off = await Invoke(blocked, new FixedFlags(false), "/admin/gebruikers/sales");
        Assert.Equal(StatusCodes.Status302Found, off.StatusCode);
        Assert.Equal(FeatureRoutes.AmbassadorsOffAccessDeniedPath, off.Location);

        var tab = await Invoke(blocked, new FixedFlags(false), "/admin/gebruikers/sales?tab=ambassadeurs");
        Assert.Equal(StatusCodes.Status302Found, tab.StatusCode);
        Assert.Equal(FeatureRoutes.AmbassadorsOffAccessDeniedPath, tab.Location);

        var missing = await Invoke(blocked, flags: null, "/ambassadeur");
        Assert.Equal(StatusCodes.Status302Found, missing.StatusCode);
        Assert.Equal(FeatureRoutes.AmbassadorsOffAccessDeniedPath, missing.Location);

        var api = await Invoke(blocked, new FixedFlags(false), "/api/ambassadeurs");
        Assert.Equal(StatusCodes.Status404NotFound, api.StatusCode);
        Assert.Contains("feature_disabled", api.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sales_admin_stays_open_when_ambassadors_are_on()
    {
        var nextCalled = false;
        var open = new AmbassadorsFeatureMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var result = await Invoke(open, new FixedFlags(true), "/admin/gebruikers/sales");
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    private static async Task<(int StatusCode, string Body, string? Location)> Invoke(
        AmbassadorsFeatureMiddleware middleware,
        IFeatureFlags? flags,
        string path)
    {
        var services = new ServiceCollection();
        if (flags is not null)
        {
            services.AddSingleton(flags);
        }

        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.Request.Path = path.Split('?')[0];
        if (path.Contains('?', StringComparison.Ordinal))
        {
            http.Request.QueryString = new QueryString(path[path.IndexOf('?', StringComparison.Ordinal)..]);
        }

        http.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(http);
        http.Response.Body.Position = 0;
        var body = await new StreamReader(http.Response.Body).ReadToEndAsync();
        return (http.Response.StatusCode, body, http.Response.Headers.Location.ToString());
    }

    private sealed class FixedFlags(bool ambassadors) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                AmbassadorsEnabled: ambassadors));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.Ambassadors && ambassadors);

        public void Invalidate()
        {
        }
    }
}
