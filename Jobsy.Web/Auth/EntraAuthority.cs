namespace Jobsy.Web.Auth;

/// <summary>Parses Microsoft Entra OIDC authority URLs (common, organizations, tenant guid).</summary>
public static class EntraAuthority
{
    /// <summary>Microsoft consumer (personal) directory tenant id.</summary>
    public const string ConsumersTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    public static bool IsMultitenantAuthoritySegment(string? tenantSegment)
    {
        if (string.IsNullOrWhiteSpace(tenantSegment))
        {
            return false;
        }

        return tenantSegment.Equals("common", StringComparison.OrdinalIgnoreCase)
               || tenantSegment.Equals("organizations", StringComparison.OrdinalIgnoreCase)
               || tenantSegment.Equals("consumers", StringComparison.OrdinalIgnoreCase);
    }

    public static string? TryGetTenantSegment(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority)
            || !Uri.TryCreate(authority.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[0] : null;
    }
}
