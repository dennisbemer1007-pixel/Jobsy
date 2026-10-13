using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

/// <summary>Legal identity from the company-settings row, with empty <c>Legal:*</c> fallback.</summary>
internal sealed class CompanyBackedLegalIdentity : ILegalIdentity
{
    private readonly IPlatformCompanySettingsService _company;

    public CompanyBackedLegalIdentity(IPlatformCompanySettingsService company) => _company = company;

    public async Task<LegalIdentitySnapshot> GetAsync(CancellationToken cancellationToken = default)
        => LegalIdentityService.Compose(null, await _company.GetAsync(cancellationToken));
}
