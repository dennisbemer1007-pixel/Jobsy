using Jobsy.Infrastructure.Data;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Opt-in EF global query filter for company-scoped entities.
/// Call at the start of employer manage endpoints so Vacancy/TokenTransaction
/// queries cannot leak other tenants even if a LINQ filter is forgotten.
/// Leave unset (null) for public listings, admin, and background jobs.
/// </summary>
public static class CompanyTenantScope
{
    public static void Enforce(JobsyDbContext db, IReadOnlyCollection<Guid>? accessibleCompanyIds)
    {
        if (accessibleCompanyIds is null)
        {
            // Admin / unrestricted — do not enable the filter.
            db.EnforceCompanyScopeIds = null;
            return;
        }

        db.EnforceCompanyScopeIds = accessibleCompanyIds as HashSet<Guid>
                                    ?? accessibleCompanyIds.ToHashSet();
    }

    public static void Clear(JobsyDbContext db)
        => db.EnforceCompanyScopeIds = null;
}
