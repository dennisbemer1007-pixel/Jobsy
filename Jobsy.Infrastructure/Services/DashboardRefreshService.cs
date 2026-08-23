using System.Security.Claims;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

public sealed class DashboardRefreshService : IDashboardRefreshService
{
    private readonly IDashboardCache _cache;
    private readonly IMetricsQueryService _metrics;
    private readonly ISalesManagerDashboardService _sales;
    private readonly IAmbassadeurDashboardService _ambassadeurs;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public DashboardRefreshService(
        IDashboardCache cache,
        IMetricsQueryService metrics,
        ISalesManagerDashboardService sales,
        IAmbassadeurDashboardService ambassadeurs,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _cache = cache;
        _metrics = metrics;
        _sales = sales;
        _ambassadeurs = ambassadeurs;
        _companyAuth = companyAuth;
        _users = users;
    }

    public async Task<DashboardRefreshResultDto> RefreshAsync(
        ClaimsPrincipal user,
        string period,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var role = _companyAuth.GetPrimaryRole(user);
        var periodKey = DashboardCacheKeys.NormalizePeriod(period);
        var generatedAt = DateTime.UtcNow;
        var cachedUntil = generatedAt.Add(_cache.TimeToLive);

        if (role is UserRole.SalesManager)
        {
            var account = await _users.FindByPrincipalAsync(user, cancellationToken)
                ?? throw new InvalidOperationException("Gebruiker niet gevonden.");
            _cache.Remove(DashboardCacheKeys.Sales(account.Id));
            var sales = await _sales.GetDashboardAsync(account.Id, cancellationToken);
            return new DashboardRefreshResultDto(generatedAt, cachedUntil, null, null, null, sales, null);
        }

        if (role is UserRole.Ambassadeur)
        {
            var account = await _users.FindByPrincipalAsync(user, cancellationToken)
                ?? throw new InvalidOperationException("Gebruiker niet gevonden.");
            _cache.Remove(DashboardCacheKeys.Ambassadeur(account.Id));
            var ambassadeur = await _ambassadeurs.GetDashboardAsync(account.Id, cancellationToken);
            return new DashboardRefreshResultDto(generatedAt, cachedUntil, null, null, null, null, ambassadeur);
        }

        if (role is not UserRole.Admin
            && role is not UserRole.BranchManager
            && role is not UserRole.RegionalManager
            && role is not UserRole.EnterpriseManager
            && role is not UserRole.Intermediary)
        {
            throw new UnauthorizedAccessException("Geen dashboard-toegang voor deze rol.");
        }

        var companyIds = await ResolveCompanyFilterAsync(user, companyId, cancellationToken);
        var scope = DashboardCacheKeys.Scope(companyIds);
        _cache.RemoveByPrefix(DashboardCacheKeys.MetricsPrefix(scope));
        _cache.RemoveByPrefix(DashboardCacheKeys.VacancyPrefix(scope));
        _cache.RemoveByPrefix(DashboardCacheKeys.ClientPrefix(scope));

        var includePlatformOnly = _companyAuth.IsAdmin(user);
        var metrics = await _metrics.GetSummaryAsync(includePlatformOnly, companyIds, periodKey, cancellationToken);
        var vacancy = await _metrics.GetVacancyPerformanceAsync(companyIds, periodKey, take: 3, cancellationToken);
        ClientPerformanceBoardDto? clients = null;
        if (role is UserRole.Admin or UserRole.Intermediary)
        {
            clients = await _metrics.GetClientPerformanceAsync(companyIds, periodKey, cancellationToken);
        }

        return new DashboardRefreshResultDto(generatedAt, cachedUntil, metrics, vacancy, clients, null, null);
    }

    private async Task<IReadOnlyCollection<Guid>?> ResolveCompanyFilterAsync(
        ClaimsPrincipal user,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (_companyAuth.IsAdmin(user) && companyId is null)
        {
            return null;
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(user, cancellationToken);
        if (companyId is not null)
        {
            if (accessible is not null && !accessible.Contains(companyId.Value) && !_companyAuth.IsAdmin(user))
            {
                throw new UnauthorizedAccessException("Geen toegang tot deze vestiging.");
            }

            return [companyId.Value];
        }

        return accessible;
    }
}
