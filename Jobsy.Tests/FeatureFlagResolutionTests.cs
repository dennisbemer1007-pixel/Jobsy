using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jobsy.Tests;

public sealed class FeatureFlagResolutionTests
{
    [Fact]
    public async Task Admin_override_wins_over_configuration_for_sensitive_experience()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jobsy:SensitiveExperienceEnabled"] = "true"
            })
            .Build();
        var sut = CreateService(db, config);
        await sut.UpdateAsync(new PlatformFeatureUpdate(SensitiveExperienceEnabled: false));

        var snap = await sut.GetAsync();
        Assert.False(snap.SensitiveExperienceEnabled);
        Assert.Contains("SensitiveExperienceEnabled", snap.AdminOverriddenFlagKeys ?? []);
    }

    [Fact]
    public async Task Configuration_used_when_no_admin_override()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jobsy:TrainingPartnersEnabled"] = "true"
            })
            .Build();
        var snap = await CreateService(db, config).GetAsync();
        Assert.True(snap.TrainingPartnersEnabled);
    }

    [Fact]
    public async Task Clear_override_restores_configuration_default()
    {
        await using var db = CreateDb();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jobsy:SensitiveExperienceEnabled"] = "true"
            })
            .Build();
        var sut = CreateService(db, config);
        await sut.UpdateAsync(new PlatformFeatureUpdate(SensitiveExperienceEnabled: false));
        await sut.UpdateAsync(new PlatformFeatureUpdate(ClearAdminFlagOverrideKeys: ["SensitiveExperienceEnabled"]));

        var snap = await sut.GetAsync();
        Assert.True(snap.SensitiveExperienceEnabled);
        Assert.DoesNotContain("SensitiveExperienceEnabled", snap.AdminOverriddenFlagKeys ?? []);
    }

    [Fact]
    public void Reference_verification_maps_to_platform_feature()
    {
        var on = new FeatureFlagSnapshot(false, true, ReferenceVerificationEnabled: true);
        Assert.True(on.IsEnabled(PlatformFeature.ReferenceVerification));
    }

    [Fact]
    public async Task Pupil_passport_v2_uses_JobsyFeatures_config_and_stored_row_when_no_override()
    {
        await using var db = CreateDb();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PupilPassportV2Enabled = true
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyFeatures:PupilPassportV2Enabled"] = "true"
            })
            .Build();

        var snap = await CreateService(db, config).GetAsync();
        Assert.True(snap.PupilPassportV2Enabled);
    }

    [Fact]
    public async Task Pupil_passport_v2_honors_stored_row_when_acceptatie_config_key_missing()
    {
        await using var db = CreateDb();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PupilPassportV2Enabled = true
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var snap = await CreateService(db, config).GetAsync();
        Assert.True(snap.PupilPassportV2Enabled);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateService(JobsyDbContext db, IConfiguration config)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);
}
