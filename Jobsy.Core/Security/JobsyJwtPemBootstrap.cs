using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Core.Security;

/// <summary>
/// Resolves JWT PEM keys for API and Web. Development and Testing may use the well-known dev pair;
/// all other environments require unique PEMs configured in the host.
/// </summary>
public static class JobsyJwtPemBootstrap
{
    public const string MissingPemMessage =
        "JobsyAuth:Jwt PEM keys are required outside Development/Testing. " +
        "Set JobsyAuth__Jwt__PublicKeyPem on the API and JobsyAuth__Jwt__PrivateKeyPem on Web " +
        "to a matching ES256 key pair unique to this environment.";

    public const string DevPemRejectedMessage =
        "JobsyAuth:Jwt must not use the repository Development PEM pair outside Development/Testing.";

    public static bool AllowDevFallback(IHostEnvironment environment)
        => environment.IsDevelopment()
           || environment.IsEnvironment("Testing");

    public static string ResolvePublicPem(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = JobsyAccessToken.NormalizePem(configuration["JobsyAuth:Jwt:PublicKeyPem"]);
        if (IsUsablePem(configured))
        {
            RejectDevelopmentPublicPemIfForbidden(configured!, environment);
            return configured!;
        }

        if (!AllowDevFallback(environment))
        {
            throw new InvalidOperationException(MissingPemMessage);
        }

        return JobsyAccessToken.DevelopmentPublicKeyPem;
    }

    public static string ResolvePrivatePem(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = JobsyAccessToken.NormalizePem(configuration["JobsyAuth:Jwt:PrivateKeyPem"]);
        if (IsUsablePem(configured))
        {
            RejectDevelopmentPrivatePemIfForbidden(configured!, environment);
            return configured!;
        }

        if (!AllowDevFallback(environment))
        {
            throw new InvalidOperationException(MissingPemMessage);
        }

        return JobsyAccessToken.DevelopmentPrivateKeyPem;
    }

    public static void RejectDevelopmentPublicPemIfForbidden(string publicPem, IHostEnvironment environment)
    {
        if (AllowDevFallback(environment))
        {
            return;
        }

        if (string.Equals(
                publicPem.Trim(),
                JobsyAccessToken.DevelopmentPublicKeyPem.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(DevPemRejectedMessage);
        }
    }

    public static void RejectDevelopmentPrivatePemIfForbidden(string privatePem, IHostEnvironment environment)
    {
        if (AllowDevFallback(environment))
        {
            return;
        }

        if (string.Equals(
                privatePem.Trim(),
                JobsyAccessToken.DevelopmentPrivateKeyPem.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(DevPemRejectedMessage);
        }
    }

    private static bool IsUsablePem(string? pem)
        => !string.IsNullOrWhiteSpace(pem) && pem.Contains("BEGIN", StringComparison.Ordinal);
}
