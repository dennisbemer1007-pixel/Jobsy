using System.Linq.Expressions;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// The only place that decides public visibility. Add new public read paths here and to
/// <c>PublicVisibilityEndpointTests</c>.
/// </summary>
public static class PublicVisibility
{
    public static bool IsCompanyPublic(Company? company)
        => company is not null
           && company.VerificationStatus == CompanyVerificationStatus.Verified;

    public static bool IsVacancyPublic(Vacancy vacancy, DateOnly today)
        => VacancyVisibilityRules.IsDateAndStatusPublic(vacancy.Status, vacancy.StartDate, vacancy.EndDate, today)
           && IsCompanyPublic(vacancy.Company)
           && (vacancy.IntermediaryCompanyId is null || IsCompanyPublic(vacancy.IntermediaryCompany));

    public static bool IsVacancyPublic(VacancyDiscoveryRecord record, DateOnly today)
        => VacancyVisibilityRules.IsDateAndStatusPublic(record.Status, record.StartDate, record.EndDate, today)
           && record.PublisherVerified;

    /// <summary>EF expression: company is Verified (for SQL filters).</summary>
    public static Expression<Func<Company, bool>> CompanyIsPublic { get; } =
        c => c.VerificationStatus == CompanyVerificationStatus.Verified;

    /// <summary>
    /// EF expression: vacancy publisher (company + optional intermediary) is Verified.
    /// </summary>
    public static Expression<Func<Vacancy, bool>> VacancyPublisherIsPublic { get; } =
        v => v.Company != null
             && v.Company.VerificationStatus == CompanyVerificationStatus.Verified
             && (v.IntermediaryCompanyId == null
                 || (v.IntermediaryCompany != null
                     && v.IntermediaryCompany.VerificationStatus == CompanyVerificationStatus.Verified));
}
