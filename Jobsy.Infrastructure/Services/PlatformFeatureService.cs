using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class PlatformFeatureService : IPlatformFeatureService
{
    private static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly JobsyDbContext _db;
    private readonly JobsyFeatureOptions _options;
    private readonly IConfiguration _configuration;

    public PlatformFeatureService(
        JobsyDbContext db,
        IOptions<JobsyFeatureOptions> options,
        IConfiguration configuration)
    {
        _db = db;
        _options = options.Value;
        _configuration = configuration;
    }

    public async Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeatureSettings.AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        return ToSnapshot(row);
    }

    public async Task<PlatformFeatureSnapshot> UpdateAsync(
        PlatformFeatureUpdate update,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = SingletonId };
            _db.PlatformFeatureSettings.Add(row);
        }

        row.VacancyContentModerationEnabled = update.VacancyContentModerationEnabled;
        row.AuthenticatorEnabled = update.AuthenticatorEnabled;
        row.ExposeRegistrationActivationLinks = update.ExposeRegistrationActivationLinks;
        row.PublicWebBaseUrl = string.IsNullOrWhiteSpace(update.PublicWebBaseUrl)
            ? null
            : update.PublicWebBaseUrl.Trim().TrimEnd('/');
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToSnapshot(row);
    }

    private PlatformFeatureSnapshot ToSnapshot(PlatformFeatureSettings? row)
    {
        var configBase = JobsyPublicUrl.NormalizeOrigin(
            _configuration["PublicWebBaseUrl"] ?? "http://localhost:5201");
        return new PlatformFeatureSnapshot(
            row?.VacancyContentModerationEnabled ?? _options.VacancyContentModerationEnabled,
            row?.AuthenticatorEnabled ?? _options.AuthenticatorEnabled,
            row?.ExposeRegistrationActivationLinks ?? _options.ExposeRegistrationActivationLinks,
            string.IsNullOrWhiteSpace(row?.PublicWebBaseUrl)
                ? configBase
                : JobsyPublicUrl.NormalizeOrigin(row.PublicWebBaseUrl),
            row?.UpdatedAtUtc);
    }
}
