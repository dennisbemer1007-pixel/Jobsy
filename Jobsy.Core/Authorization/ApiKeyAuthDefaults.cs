namespace Jobsy.Core.Authorization;

public static class ApiKeyAuthDefaults
{
    public const string AuthenticationScheme = "ApiKey";
    public const string HeaderName = "X-API-Key";

    /// <summary>Claim type for the authenticated ApiKeys row id.</summary>
    public const string ApiKeyIdClaim = "api_key_id";
}
