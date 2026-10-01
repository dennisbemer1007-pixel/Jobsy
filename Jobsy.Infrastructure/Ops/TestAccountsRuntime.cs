using Jobsy.Core.Ops;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Ops;

public static class TestAccountsRuntimeFactory
{
    public static ITestAccountsRuntime Create(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger logger,
        bool isWebRuntime = false)
    {
        var input = TestAccountGuardInputFactory.FromConfiguration(
            configuration,
            hostEnvironment,
            isWebRuntime);
        var runtime = TestAccountsRuntimeState.FromInput(input);
        logger.LogInformation(
            "Test accounts runtime: {Status} ({Codes})",
            runtime.IsActive ? "active" : "inactive",
            runtime.StatusCodes);
        return runtime;
    }
}
