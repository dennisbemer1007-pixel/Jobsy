using Jobsy.Core.Hosting;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Hosting;

public static class AcceptatieConfigurationExtensions
{
    public static IConfigurationBuilder AddJobsyAcceptatieAppSettings(this IConfigurationBuilder builder)
    {
        var deployment = Environment.GetEnvironmentVariable("Lobsy__DeploymentEnvironment")
                         ?? Environment.GetEnvironmentVariable("Lobsy:DeploymentEnvironment");
        if (string.Equals(deployment, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase))
        {
            builder.AddJsonFile("appsettings.Acceptatie.json", optional: true, reloadOnChange: false);
        }

        return builder;
    }
}
