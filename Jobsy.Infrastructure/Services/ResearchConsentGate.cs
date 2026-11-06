using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class ResearchConsentGate : IResearchConsentGate
{
    private readonly JobsyDbContext _db;
    private readonly JobsyFeatureOptions _options;

    public ResearchConsentGate(JobsyDbContext db, IOptions<JobsyFeatureOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ResearchConsentEnabled)
        {
            return true;
        }

        var row = await _db.PlatformFeatureSettings.AsNoTracking()
            .Select(p => (bool?)p.ResearchConsentEnabled)
            .FirstOrDefaultAsync(cancellationToken);
        return row == true;
    }
}
