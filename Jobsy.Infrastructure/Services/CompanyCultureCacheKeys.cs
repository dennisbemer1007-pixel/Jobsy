namespace Jobsy.Infrastructure.Services;

/// <summary>Shared memory-cache keys for employer culture profiles (lookup + eviction on save).</summary>
public static class CompanyCultureCacheKeys
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private const string Prefix = "company-culture:";

    public static string ForCompany(Guid companyId) => Prefix + companyId.ToString("D");
}
