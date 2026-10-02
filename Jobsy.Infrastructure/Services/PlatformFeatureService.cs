using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class PlatformFeatureService : IPlatformFeatureService
{
    private static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly JobsyDbContext _db;
    private readonly JobsyFeatureOptions _options;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache? _cache;

    public PlatformFeatureService(
        JobsyDbContext db,
        IOptions<JobsyFeatureOptions> options,
        IConfiguration configuration,
        IMemoryCache? cache = null)
    {
        _db = db;
        _options = options.Value;
        _configuration = configuration;
        _cache = cache;
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
        var isNew = row is null;
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = SingletonId };
            _db.PlatformFeatureSettings.Add(row);
            ApplyDefaultsForInsert(row);
        }

        if (update.VacancyContentModerationEnabled is bool moderation)
        {
            row.VacancyContentModerationEnabled = moderation;
        }

        if (update.AuthenticatorEnabled is bool authenticator)
        {
            row.AuthenticatorEnabled = authenticator;
        }

        // ExposeRegistrationActivationLinks ignored since auth 06 (activation links removed).

        if (update.InactiveCompanyDays is int inactiveDays)
        {
            row.InactiveCompanyDays = Math.Clamp(inactiveDays, 30, 730);
        }

        if (update.SessionInactivityTimeoutMinutes is int timeout)
        {
            row.SessionInactivityTimeoutMinutes = SessionSecurityRules.ClampTimeoutMinutes(timeout);
        }

        if (update.MinimumSessionVersion is int minSession)
        {
            row.MinimumSessionVersion = Math.Max(0, minSession);
        }

        if (update.SupportAccessNotifyAdmins is bool notifyAdmins)
        {
            row.SupportAccessNotifyAdmins = notifyAdmins;
        }

        if (update.SupportAccessNotifySubject is bool notifySubject)
        {
            row.SupportAccessNotifySubject = notifySubject;
        }

        if (update.CandidateInsightsEnabled is bool insightsEnabled)
        {
            row.CandidateInsightsEnabled = insightsEnabled;
        }

        if (update.CandidateInsightsUnlockDays is int unlockDays)
        {
            row.CandidateInsightsUnlockDays = CandidateInsightsAccess.ClampUnlockDays(unlockDays);
        }
        else if (isNew)
        {
            row.CandidateInsightsUnlockDays = CandidateInsightsAccess.DefaultUnlockDays;
        }

        if (update.CandidateInsightsUnlockPerBranch is bool perBranch)
        {
            row.CandidateInsightsUnlockPerBranch = perBranch;
        }

        if (update.SchoolsEnabled is bool schoolsEnabled)
        {
            row.SchoolsEnabled = schoolsEnabled;
        }

        if (update.SchoolPerCodeResultsEnabled is bool perCode)
        {
            row.SchoolPerCodeResultsEnabled = perCode;
        }

        if (update.SchoolRetentionCutoffMonth is int || update.SchoolRetentionCutoffDay is int)
        {
            var month = update.SchoolRetentionCutoffMonth ?? row.SchoolRetentionCutoffMonth;
            var day = update.SchoolRetentionCutoffDay ?? row.SchoolRetentionCutoffDay;
            Jobsy.Core.Scholen.SchoolYear.ValidateCutoff(month, day);
            row.SchoolRetentionCutoffMonth = month;
            row.SchoolRetentionCutoffDay = day;
        }

        if (update.AmbassadorsEnabled is bool ambassadorsEnabled)
        {
            row.AmbassadorsEnabled = ambassadorsEnabled;
        }
        if (update.EmployersEnabled is bool employersEnabled)
        {
            row.EmployersEnabled = employersEnabled;
        }

        if (update.CandidatePassportEnabled is bool passportEnabled)
        {
            row.CandidatePassportEnabled = passportEnabled;
        }

        if (update.MaintenanceEnabled is bool maintenanceEnabled)
        {
            row.MaintenanceEnabled = maintenanceEnabled;
        }

        if (update.ClearMaintenanceExpectedEndUtc)
        {
            row.MaintenanceExpectedEndUtc = null;
        }
        else if (update.MaintenanceExpectedEndUtc is DateTime expectedEnd)
        {
            row.MaintenanceExpectedEndUtc = DateTime.SpecifyKind(expectedEnd, DateTimeKind.Utc);
        }

        if (update.MaintenanceNote is not null)
        {
            row.MaintenanceNote = MaintenanceRules.NormalizeNote(update.MaintenanceNote);
        }

        // Explicit clear → null. Explicit date → set. Otherwise preserve (or launch default on insert)
        // so session-timeout-only PUTs do not silently disable the free-publish promo.
        if (update.ClearFreePublishUntil)
        {
            row.FreePublishUntil = null;
        }
        else if (update.FreePublishUntil is DateOnly until)
        {
            row.FreePublishUntil = until;
        }
        else if (isNew)
        {
            row.FreePublishUntil = FreePublishRules.DefaultUntil;
        }

        // Null PublicWebBaseUrl = keep. Non-empty = set after validation.
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

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _cache?.Remove(FeatureFlags.CacheKey);
        _cache?.Remove(MaintenanceRules.CacheKey);
        return ToSnapshot(row);
    }

    private void ApplyDefaultsForInsert(PlatformFeatureSettings row)
    {
        row.VacancyContentModerationEnabled = _options.VacancyContentModerationEnabled;
        row.AuthenticatorEnabled = _options.AuthenticatorEnabled;
        row.ExposeRegistrationActivationLinks = false;
        row.InactiveCompanyDays = 120;
        row.SessionInactivityTimeoutMinutes = SessionSecurityRules.DefaultInactivityTimeoutMinutes;
        row.SupportAccessNotifyAdmins = false;
        row.SupportAccessNotifySubject = false;
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
        // No DB row yet → launch default (free publish until 18-11-2026).
        // Explicit null on an existing row means admin turned the promo off.
        var freeUntil = row is null
            ? FreePublishRules.DefaultUntil
            : row.FreePublishUntil;
        return new PlatformFeatureSnapshot(
            row?.VacancyContentModerationEnabled ?? _options.VacancyContentModerationEnabled,
            row?.AuthenticatorEnabled ?? _options.AuthenticatorEnabled,
            false,
            string.IsNullOrWhiteSpace(row?.PublicWebBaseUrl)
                ? configBase
                : JobsyPublicUrl.NormalizeOrigin(row.PublicWebBaseUrl),
            row?.UpdatedAtUtc,
            row?.InactiveCompanyDays is > 0 ? row.InactiveCompanyDays : 120,
            SessionSecurityRules.ClampTimeoutMinutes(
                row?.SessionInactivityTimeoutMinutes
                ?? SessionSecurityRules.DefaultInactivityTimeoutMinutes),
            freeUntil,
            row?.MinimumSessionVersion ?? 0,
            row?.SupportAccessNotifyAdmins ?? false,
            row?.SupportAccessNotifySubject ?? false,
            row?.CandidateInsightsEnabled ?? true,
            row is null
                ? CandidateInsightsAccess.DefaultUnlockDays
                : CandidateInsightsAccess.ClampUnlockDays(row.CandidateInsightsUnlockDays),
            row?.CandidateInsightsUnlockPerBranch ?? false,
            row?.SchoolsEnabled ?? false,
            row?.SchoolPerCodeResultsEnabled ?? true,
            row?.SchoolRetentionCutoffMonth is >= 1 and <= 12
                ? row.SchoolRetentionCutoffMonth
                : 7,
            row?.SchoolRetentionCutoffDay is >= 1 and <= 31
                ? row.SchoolRetentionCutoffDay
                : 31,
            row?.AmbassadorsEnabled ?? false,
            row?.EmployersEnabled ?? true,
            row?.CandidatePassportEnabled ?? false,
            row?.MaintenanceEnabled ?? false,
            row?.MaintenanceExpectedEndUtc is DateTime end
                ? DateTime.SpecifyKind(end, DateTimeKind.Utc)
                : null,
            row?.MaintenanceNote);
    }
}
