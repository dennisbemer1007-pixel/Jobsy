using Jobsy.Infrastructure.Ops;

namespace Jobsy.Api.Hosting;

public static class ApiHealthEndpoints
{
    public static object BuildPayload() => new
    {
        status = "ok",
        commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT")
                 ?? Environment.GetEnvironmentVariable("GIT_COMMIT")
                 ?? "local",
        acceptatieSollicitatiesDemo = AcceptatieSollicitatiesDemoStartupState.Snapshot()
    };
}
