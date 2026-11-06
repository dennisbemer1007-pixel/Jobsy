namespace Jobsy.Core.Hosting;

/// <summary>Resolved deployment badge label (singleton from PublicWebBaseUrl / Deployment:Label).</summary>
public sealed record DeploymentEnvironmentLabel(string Value)
{
    public override string ToString() => Value;
}
