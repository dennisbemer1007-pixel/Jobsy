using Jobsy.Core.Hosting;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Core.Media;

/// <summary>
/// Resolves the public web origin for API-side fetches of <c>/images/…</c> (map popup photos, etc.).
/// </summary>
public static class VacancyPublicWebOrigin
{
    public const string AcceptatieDefault = "https://acceptatie.lobsy.nl";
    public const string ProductionDefault = "https://lobsy.nl";
    public const string LocalDefault = "http://localhost:5201";

    public static string Resolve(IConfiguration configuration)
    {
        var configured = JobsyPublicUrl.NormalizeOrigin(configuration["PublicWebBaseUrl"]);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var deployment = configuration["Lobsy:DeploymentEnvironment"]
                         ?? configuration["Lobsy__DeploymentEnvironment"];
        var label = DeploymentEnvironment.Resolve(null, deployment, null);
        return label switch
        {
            DeploymentEnvironment.Acceptatie => AcceptatieDefault,
            DeploymentEnvironment.Productie => ProductionDefault,
            _ => LocalDefault
        };
    }
}
