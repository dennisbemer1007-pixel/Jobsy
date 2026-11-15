using Jobsy.Core.Entities;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.WerkgeverApk;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.WerkgeverApk;

internal static class WerkgeverApkTestSupport
{
    internal sealed class FlagsOn : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            EmployerLoopbaanApkEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    internal sealed class FlagsOff : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = FeatureFlagSnapshot.Defaults;

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public void Invalidate()
        {
        }
    }

    internal static async Task<(JobsyDbContext Db, WerkgeverApkTeam Team, WerkgeverApkEmployeeCode Code, string PlainCode)> SeedTeamWithCodeAsync(
        bool companyEnabled = true)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddDataProtection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<IWerkgeverApkEmployeeCodeService, WerkgeverApkEmployeeCodeService>();
        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<JobsyDbContext>();
        var codes = sp.GetRequiredService<IWerkgeverApkEmployeeCodeService>();

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Demo Logistiek B.V.",
            KvkNumber = "12345678",
            Address = "Magazijnweg 1",
            Location = new GeoPoint(52.07, 4.30),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow,
            LoopbaanApkEnabled = companyEnabled
        };
        var team = new WerkgeverApkTeam
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Name = "Team Logistiek",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };
        db.Companies.Add(company);
        db.WerkgeverApkTeams.Add(team);
        await db.SaveChangesAsync();

        var list = await codes.GenerateAsync(1, team);
        var code = list[0];
        var plain = codes.Unprotect(code.CodeProtected)!;
        return (db, team, code, plain);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}