using Jobsy.Core.Hosting;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Infrastructure.Hosting;

/// <summary>
/// Acceptatie uses <c>ASPNETCORE_ENVIRONMENT=Production</c> on Render; load repo
/// <c>appsettings.Acceptatie.json</c> when deployment is Acceptatie.
/// </summary>
public static class AcceptatieConfigurationExtensions
{
    public static IConfigurationBuilder AddJobsyAcceptatieAppSettings(this IConfigurationBuilder builder)
    {
        if (IsAcceptatieDeploymentFromEnvironment())
        {
            builder.AddJsonFile("appsettings.Acceptatie.json", optional: true, reloadOnChange: false);
        }

        return builder;
    }

    public static bool IsAcceptatieDeployment(IConfiguration configuration)
    {
        var deployment = configuration["Lobsy:DeploymentEnvironment"]
                         ?? configuration["Lobsy__DeploymentEnvironment"]
                         ?? configuration["Deployment:Label"];
        return string.Equals(deployment, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAcceptatieDeploymentFromEnvironment()
    {
        var deployment = Environment.GetEnvironmentVariable("Lobsy__DeploymentEnvironment")
                         ?? Environment.GetEnvironmentVariable("Lobsy:DeploymentEnvironment");
        return string.Equals(deployment, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase);
    }
}
