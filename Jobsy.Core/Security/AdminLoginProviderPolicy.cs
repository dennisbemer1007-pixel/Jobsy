using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

/// <summary>
/// Admins may only use Microsoft work/school accounts or password+2FA — never Google
/// or a personal Microsoft account (Dennis D7 / D15).
/// </summary>
public static class AdminLoginProviderPolicy
{
    /// <summary>Entra consumer / personal Microsoft accounts tenant.</summary>
    public const string PersonalMicrosoftTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    public static bool IsAllowed(
        UserRole role,
        string? provider,
        string? entraTenantId,
        IReadOnlyCollection<string> allowedAdminTenants)
    {
        if (role != UserRole.Admin)
        {
            return true;
        }

        var normalized = NormalizeProvider(provider);
        if (normalized is null)
        {
            return true; // local password path
        }

        if (string.Equals(normalized, "google", StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(normalized, "entra", StringComparison.Ordinal))
        {
            if (string.Equals(entraTenantId, PersonalMicrosoftTenantId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (allowedAdminTenants.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(entraTenantId))
                {
                    return false;
                }

                return allowedAdminTenants.Any(t =>
                    string.Equals(t.Trim(), entraTenantId.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return true;
        }

        return false;
    }

    public static IReadOnlyList<string> ParseAllowedTenants(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    private static string? NormalizeProvider(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return null;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "entra" or "microsoft" or "microsoftentra" or "oidc" => "entra",
            "google" or "googleentra" => "google",
            var p => p
        };
    }
}
