using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Uwv;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.UwvMockups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jobsy.Tests;

public sealed class UwvFeaturesTests
{
    [Fact]
    public void Ondernemer_result_next_steps_kvk_opens_in_new_tab()
    {
        var kvk = UwvMockupCopy.Ondernemer.ResultNextSteps("nl").Single(s => s.Href.Contains("kvk.nl", StringComparison.Ordinal));
        Assert.True(kvk.External);
        Assert.StartsWith("https://", kvk.Href, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FeatureFlagSnapshot_uwv_features_off_by_default()
    {
        var snap = FeatureFlagSnapshot.Defaults;
        Assert.False(snap.UwvFeaturesEnabled);
        Assert.False(snap.IsEnabled(PlatformFeature.UwvFeatures));
    }

    [Fact]
    public async Task PlatformFeatureService_round_trips_uwv_features()
    {
        await using var db = CreateDb();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(UwvFeaturesEnabled: true));
        Assert.True(updated.UwvFeaturesEnabled);

        var again = await sut.GetAsync();
        Assert.True(again.UwvFeaturesEnabled);
    }

    [Fact]
    public async Task UwvCandidateToolkitService_persists_ondernemer_and_drijfveren()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new Jobsy.Core.Entities.User
        {
            Id = userId,
            Email = $"uwv-{userId:N}@test.local",
            FullName = "Sanne de Vries",
            Role = Jobsy.Core.Enums.UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var features = new StubUwvFeaturesOn();
        var sut = new UwvCandidateToolkitService(db, null!, features);

        var saved = await sut.SaveOndernemerAsync(userId, [4, 4, 3, 4, 3, 4], CancellationToken.None);
        Assert.Equal("explore", saved.OndernemerOutcomeKey);

        var drivers = await sut.SaveDrijfverenAsync(
            userId,
            [UwvDriverKeys.Security, UwvDriverKeys.Growth],
            CancellationToken.None);
        Assert.Equal(2, drivers.DrijfverenDriverKeys.Count);

        var share = await sut.CreateShareLinkAsync(
            userId,
            [UwvAdviseurSectionKeys.Drijfveren, UwvAdviseurSectionKeys.Competences],
            "nl",
            CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(share.Token));

        var view = await sut.GetShareViewAsync(share.Token, CancellationToken.None);
        Assert.NotNull(view);
        Assert.Equal("Sanne de Vries", view!.CandidateDisplayName);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubUwvFeaturesOn : IPlatformFeatureService
    {
        public Task<Jobsy.Core.Interfaces.PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new Jobsy.Core.Interfaces.PlatformFeatureSnapshot(
                VacancyContentModerationEnabled: false,
                AuthenticatorEnabled: false,
                PublicWebBaseUrl: "https://lobsy.nl",
                UpdatedAtUtc: DateTime.UtcNow,
                ReferenceVerificationEnabled: false,
                UwvFeaturesEnabled: true));

        public Task<Jobsy.Core.Interfaces.PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}
