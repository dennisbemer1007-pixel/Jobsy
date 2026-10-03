using Jobsy.Core.Email;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jobsy.Tests;

public class EmployersFeatureSuppressionTests
{
    [Fact]
    public async Task Email_stub_suppresses_employer_templates_when_off()
    {
        await using var db = CreateDb();
        var flags = new FixedFlags(employers: false);
        var stub = new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance, flags);

        var result = await stub.SendAsync(new EmailMessage(
            "a@b.nl", "Sollicitatie", "<p>x</p>", "ApplicationConfirmation"));
        Assert.Equal(EmailDeliveryKind.Stub, result.Kind);
        Assert.Empty(db.PlatformLogs);

        var keep = await stub.SendAsync(new EmailMessage(
            "a@b.nl", "Uitschrijf", "<p>x</p>", "AccountUnsubscribeVerification"));
        Assert.Equal(EmailDeliveryKind.Stub, keep.Kind);
        Assert.Single(db.PlatformLogs);
    }

    [Fact]
    public async Task Email_stub_sends_employer_templates_when_on()
    {
        await using var db = CreateDb();
        var flags = new FixedFlags(employers: true);
        var stub = new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance, flags);
        await stub.SendAsync(new EmailMessage(
            "a@b.nl", "Sollicitatie", "<p>x</p>", "ApplicationConfirmation"));
        Assert.Single(db.PlatformLogs);
    }

    [Fact]
    public async Task EmployersJobGate_logs_state_change_and_skips_when_off()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: false));
        await using var provider = services.BuildServiceProvider();
        var gate = new EmployersJobGate(NullLogger.Instance, "TestJob");
        Assert.False(await gate.ShouldRunAsync(provider));
        // Second call keeps OFF — still false (no catch-up).
        Assert.False(await gate.ShouldRunAsync(provider));
    }

    [Fact]
    public async Task EmployersJobGate_runs_when_on()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFeatureFlags>(new FixedFlags(employers: true));
        await using var provider = services.BuildServiceProvider();
        var gate = new EmployersJobGate(NullLogger.Instance, "TestJob");
        Assert.True(await gate.ShouldRunAsync(provider));
    }

    [Fact]
    public void Sitemap_static_paths_drop_vacancy_urls_when_employers_off()
    {
        var off = new FeatureFlagSnapshot(false, false);
        var paths = Jobsy.Web.Seo.PageSeoCatalog.StaticIndexablePathsFor(off);
        Assert.DoesNotContain(paths, p => p.Contains("vacancies", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("/banenkaart", paths);
        Assert.Contains("/", paths);
        Assert.Contains("/ontdek", paths);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FixedFlags(bool employers) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.Employers && employers);

        public void Invalidate()
        {
        }
    }
}
