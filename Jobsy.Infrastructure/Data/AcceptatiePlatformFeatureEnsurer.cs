using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Infrastructure.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Keeps acceptatie platform flags aligned with <c>appsettings.Acceptatie.json</c> when the DB row
/// still has admin overrides off (common cause of feature-gated 404s on acc).
/// </summary>
internal static class AcceptatiePlatformFeatureEnsurer
{
    private static readonly Guid PlatformSettingsId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    internal static async Task EnsureActivityLogEnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:ActivityLogEnabled", false))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.ActivityLogEnabled = true;
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with ActivityLogEnabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("ActivityLogEnabled") && !row.ActivityLogEnabled;
        if (!row.ActivityLogEnabled || lockedOff)
        {
            row.ActivityLogEnabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "ActivityLogEnabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced ActivityLogEnabled=true (removed admin override lock when present).");
        }
    }
}
