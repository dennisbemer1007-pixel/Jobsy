using Jobsy.Api.Security;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class CandidateApplyRateLimitSettingsTests
{
    [Fact]
    public async Task Platform_features_default_apply_limits_match_product_defaults()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);
        var snap = await sut.GetAsync();
        Assert.Equal(CandidateApplyRateLimitRules.DefaultPerHour, snap.CandidateApplyPerHourLimit);
        Assert.Equal(CandidateApplyRateLimitRules.DefaultPerDay, snap.CandidateApplyPerDayLimit);
        Assert.Equal(CandidateApplyRateLimitRules.DefaultPerMinute, snap.CandidateApplyPerMinuteLimit);
    }

    [Fact]
    public async Task Platform_features_persist_and_clamp_apply_rate_limits()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            CandidateApplyPerHourLimit: 12,
            CandidateApplyPerDayLimit: 40,
            CandidateApplyPerMinuteLimit: 0));

        Assert.Equal(12, updated.CandidateApplyPerHourLimit);
        Assert.Equal(40, updated.CandidateApplyPerDayLimit);
        Assert.Equal(CandidateApplyRateLimitRules.MinLimit, updated.CandidateApplyPerMinuteLimit);
    }

    [Fact]
    public async Task Updated_hourly_admin_cap_is_used_by_triple_window_limiter()
    {
        await using var db = CreateDb();
        var sut = CreateService(db);
        await sut.UpdateAsync(new PlatformFeatureUpdate(CandidateApplyPerHourLimit: 2));
        var snap = await sut.GetAsync();

        using var limiter = new TripleWindowRateLimiter(
            burstPermitLimit: 100,
            burstWindow: TimeSpan.FromMinutes(1),
            hourlyPermitLimit: snap.CandidateApplyPerHourLimit,
            hourlyWindow: TimeSpan.FromHours(1),
            dailyPermitLimit: snap.CandidateApplyPerDayLimit,
            dailyWindow: TimeSpan.FromDays(1));

        Assert.True(limiter.AttemptAcquire(1).IsAcquired);
        Assert.True(limiter.AttemptAcquire(1).IsAcquired);
        using var blocked = limiter.AttemptAcquire(1);
        Assert.False(blocked.IsAcquired);
    }

    [Fact]
    public void Platform_settings_catalog_exposes_apply_rate_fields()
    {
        Assert.True(PlatformSettingsCatalog.FieldExists("CandidateApplyPerHourLimit"));
        Assert.True(PlatformSettingsCatalog.FieldExists("CandidateApplyPerDayLimit"));
        Assert.True(PlatformSettingsCatalog.FieldExists("CandidateApplyPerMinuteLimit"));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            })
            .Build();
        return new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);
    }
}
