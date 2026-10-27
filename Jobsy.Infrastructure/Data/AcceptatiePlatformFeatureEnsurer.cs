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
public static class AcceptatiePlatformFeatureEnsurer
{
    private static readonly Guid PlatformSettingsId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static async Task EnsureAcceptatieFeatureFlagsAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmployersEnabledAsync(db, configuration, logger, cancellationToken);
        await EnsureActivityLogEnabledAsync(db, configuration, logger, cancellationToken);
        await EnsureKandidatenkaartEnabledAsync(db, configuration, logger, cancellationToken);
    }

    internal static async Task EnsureEmployersEnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:EmployersEnabled", true))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.EmployersEnabled = true;
            row.ActivityLogEnabled = configuration.GetValue("JobsyFeatures:ActivityLogEnabled", false);
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with EmployersEnabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("EmployersEnabled") && row.EmployersEnabled != true;
        if (row.EmployersEnabled != true || lockedOff)
        {
            row.EmployersEnabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "EmployersEnabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced EmployersEnabled=true (removed admin override lock when present).");
        }
    }

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

    internal static async Task EnsureKandidatenkaartEnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:KandidatenkaartEnabled", false))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.KandidatenkaartEnabled = true;
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with KandidatenkaartEnabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("KandidatenkaartEnabled") && !row.KandidatenkaartEnabled;
        if (!row.KandidatenkaartEnabled || lockedOff)
        {
            row.KandidatenkaartEnabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "KandidatenkaartEnabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced KandidatenkaartEnabled=true (removed admin override lock when present).");
        }
    }
}
