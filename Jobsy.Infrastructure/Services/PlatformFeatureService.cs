using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
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
        if (!string.IsNullOrWhiteSpace(update.PublicWebBaseUrl))
        {
            var normalized = JobsyPublicUrl.NormalizeOrigin(update.PublicWebBaseUrl);
            if (!IsAllowedPublicOrigin(normalized))
            {
                throw new ArgumentException(
                    "PublicWebBaseUrl moet https zijn en overeenkomen met de geconfigureerde publieke origin of CORS-origins.");
            }

            row.PublicWebBaseUrl = normalized.TrimEnd('/');
        }
        else
        {
            row.PublicWebBaseUrl = null;
        }
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToSnapshot(row);
    }

    private bool IsAllowedPublicOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var isLocalHttp = uri.Scheme == Uri.UriSchemeHttp
                          && uri.Host is "localhost" or "127.0.0.1" or "::1";
        if (uri.Scheme != Uri.UriSchemeHttps && !isLocalHttp)
        {
            return false;
        }

        if (!HtmlSanitize.IsSafeHttpsUrl(origin) && !isLocalHttp)
        {
            return false;
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "http://localhost:5201",
            "https://localhost:5201"
        };

        var configBase = JobsyPublicUrl.NormalizeOrigin(
            _configuration["PublicWebBaseUrl"] ?? "http://localhost:5201");
        if (!string.IsNullOrWhiteSpace(configBase))
        {
            allowed.Add(configBase.TrimEnd('/'));
        }

        foreach (var child in _configuration.GetSection("Cors:AllowedOrigins").GetChildren())
        {
            var o = JobsyPublicUrl.NormalizeOrigin(child.Value);
            if (!string.IsNullOrWhiteSpace(o))
            {
                allowed.Add(o.TrimEnd('/'));
            }
        }

        return allowed.Contains(origin.TrimEnd('/'));
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
