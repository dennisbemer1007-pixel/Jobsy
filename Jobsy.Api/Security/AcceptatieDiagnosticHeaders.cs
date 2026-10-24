using Jobsy.Core.Hosting;

namespace Jobsy.Api.Security;

/// <summary>
/// Exposes safe exception hints on Acceptatie only (never production).
/// </summary>
public static class AcceptatieDiagnosticHeaders
{
    public const string ErrorHeaderName = "X-Jobsy-Error";

    public static void TrySet(HttpResponse response, DeploymentEnvironmentLabel deployment, Exception ex)
    {
        if (!IsAcceptatie(deployment))
        {
            return;
        }

        var text = Format(ex);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        response.Headers[ErrorHeaderName] = text;
    }

    internal static bool IsAcceptatie(DeploymentEnvironmentLabel deployment)
        => string.Equals(deployment.Value, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase);

    private static string Format(Exception ex)
    {
        var message = ex.Message;
        if (ex.InnerException is { } inner)
        {
            message += " | " + inner.GetType().Name + ": " + inner.Message;
        }

        message = message.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        if (message.Length > 480)
        {
            message = message[..480];
        }

        return ex.GetType().Name + ": " + message;
    }
}
