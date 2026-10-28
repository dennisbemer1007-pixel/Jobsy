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
        await EnsureSchoolsEnabledAsync(db, configuration, logger, cancellationToken);
        await EnsurePupilPassportV2EnabledAsync(db, configuration, logger, cancellationToken);
        await EnsureTestItemExamplesEnabledAsync(db, configuration, logger, cancellationToken);
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

    internal static async Task EnsureSchoolsEnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:SchoolsEnabled", false))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.SchoolsEnabled = true;
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with SchoolsEnabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("SchoolsEnabled") && !row.SchoolsEnabled;
        if (!row.SchoolsEnabled || lockedOff)
        {
            row.SchoolsEnabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "SchoolsEnabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced SchoolsEnabled=true (removed admin override lock when present).");
        }
    }

    internal static async Task EnsurePupilPassportV2EnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:SchoolsEnabled", false))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:PupilPassportV2Enabled", true))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.PupilPassportV2Enabled = true;
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with PupilPassportV2Enabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("PupilPassportV2Enabled") && !row.PupilPassportV2Enabled;
        if (!row.PupilPassportV2Enabled || lockedOff)
        {
            row.PupilPassportV2Enabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "PupilPassportV2Enabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced PupilPassportV2Enabled=true (removed admin override lock when present).");
        }
    }

    internal static async Task EnsureTestItemExamplesEnabledAsync(
        JobsyDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!AcceptatieConfigurationExtensions.IsAcceptatieDeployment(configuration))
        {
            return;
        }

        if (!configuration.GetValue("JobsyFeatures:TestItemExamplesEnabled", false))
        {
            return;
        }

        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = PlatformSettingsId };
            row.TestItemExamplesEnabled = true;
            db.PlatformFeatureSettings.Add(row);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Acceptatie: created platform settings with TestItemExamplesEnabled=true.");
            return;
        }

        var overrides = PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson);
        var lockedOff = overrides.Contains("TestItemExamplesEnabled") && !row.TestItemExamplesEnabled;
        if (!row.TestItemExamplesEnabled || lockedOff)
        {
            row.TestItemExamplesEnabled = true;
            row.AdminFlagOverridesJson =
                PlatformFeatureFlagOverrides.RemoveKey(row.AdminFlagOverridesJson, "TestItemExamplesEnabled");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Acceptatie: synced TestItemExamplesEnabled=true (removed admin override lock when present).");
        }
    }
}
