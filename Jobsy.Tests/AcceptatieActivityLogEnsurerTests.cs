using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public sealed class AcceptatieActivityLogEnsurerTests
{
    [Fact]
    public async Task EnsureActivityLogEnabled_turns_on_locked_off_acceptatie_row()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ActivityLogEnabled = false,
            AdminFlagOverridesJson = PlatformFeatureFlagOverrides.AddKey(null, "ActivityLogEnabled")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:ActivityLogEnabled"] = "true"
            })
            .Build();

        await AcceptatiePlatformFeatureEnsurer.EnsureActivityLogEnabledAsync(
            db,
            config,
            NullLogger.Instance);

        var row = await db.PlatformFeatureSettings.SingleAsync();
        Assert.True(row.ActivityLogEnabled);
        Assert.DoesNotContain(
            "ActivityLogEnabled",
            PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson));
    }

    [Fact]
    public async Task EnsureTestItemExamplesEnabled_turns_on_locked_off_acceptatie_row()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            TestItemExamplesEnabled = false,
            AdminFlagOverridesJson = PlatformFeatureFlagOverrides.AddKey(null, "TestItemExamplesEnabled")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:TestItemExamplesEnabled"] = "true"
            })
            .Build();

        await AcceptatiePlatformFeatureEnsurer.EnsureTestItemExamplesEnabledAsync(
            db,
            config,
            NullLogger.Instance);

        var row = await db.PlatformFeatureSettings.SingleAsync();
        Assert.True(row.TestItemExamplesEnabled);
    }

    [Fact]
    public async Task EnsureEmployersEnabled_turns_on_locked_off_acceptatie_row()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            EmployersEnabled = false,
            AdminFlagOverridesJson = PlatformFeatureFlagOverrides.AddKey(null, "EmployersEnabled")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:EmployersEnabled"] = "true"
            })
            .Build();

        await AcceptatiePlatformFeatureEnsurer.EnsureEmployersEnabledAsync(
            db,
            config,
            NullLogger.Instance);

        var row = await db.PlatformFeatureSettings.SingleAsync();
        Assert.True(row.EmployersEnabled);
        Assert.DoesNotContain(
            "EmployersEnabled",
            PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson));
    }

    [Fact]
    public async Task EnsureSchoolsEnabled_turns_on_locked_off_acceptatie_row()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            SchoolsEnabled = false,
            AdminFlagOverridesJson = PlatformFeatureFlagOverrides.AddKey(null, "SchoolsEnabled")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true"
            })
            .Build();

        await AcceptatiePlatformFeatureEnsurer.EnsureSchoolsEnabledAsync(
            db,
            config,
            NullLogger.Instance);

        var row = await db.PlatformFeatureSettings.SingleAsync();
        Assert.True(row.SchoolsEnabled);
        Assert.DoesNotContain(
            "SchoolsEnabled",
            PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson));
    }

    [Fact]
    public async Task EnsurePupilPassportV2Enabled_turns_on_locked_off_acceptatie_row()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PupilPassportV2Enabled = false,
            AdminFlagOverridesJson = PlatformFeatureFlagOverrides.AddKey(null, "PupilPassportV2Enabled")
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true",
                ["JobsyFeatures:PupilPassportV2Enabled"] = "true"
            })
            .Build();

        await AcceptatiePlatformFeatureEnsurer.EnsurePupilPassportV2EnabledAsync(
            db,
            config,
            NullLogger.Instance);

        var row = await db.PlatformFeatureSettings.SingleAsync();
        Assert.True(row.PupilPassportV2Enabled);
        Assert.DoesNotContain(
            "PupilPassportV2Enabled",
            PlatformFeatureFlagOverrides.Parse(row.AdminFlagOverridesJson));
    }
}
