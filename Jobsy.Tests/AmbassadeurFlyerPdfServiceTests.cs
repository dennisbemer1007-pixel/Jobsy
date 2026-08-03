using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class AmbassadeurFlyerPdfServiceTests
{
    [Fact]
    public async Task Render_Candidate_And_Entrepreneur_Flyers_Produce_Pdf_Bytes()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = userId,
            Email = "flyer-am@test.local",
            FullName = "Flyer AM",
            Role = UserRole.Ambassadeur,
            IsActive = true
        });
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackingCode = "AM-FLYER1",
            BaseCommissionPercentage = 5m,
            AgreementSignedAt = now,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CompanyName = "Flyer BV",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "Straat 1",
            PostalCode = "1234AB",
            City = "Naaldwijk"
        });
        await db.SaveChangesAsync();

        var service = new AmbassadeurFlyerPdfService(
            db,
            new FakeSalesCommercial(),
            new FakeCompanySettings(),
            new FakeFeatures());

        var candidate = await service.RenderAsync("AM-FLYER1", AmbassadeurFlyerKind.Candidate);
        var entrepreneur = await service.RenderAsync("AM-FLYER1", AmbassadeurFlyerKind.Entrepreneur);

        Assert.True(candidate.Length > 500);
        Assert.True(entrepreneur.Length > 500);
        Assert.Equal((byte)'%', candidate[0]);
        Assert.Equal((byte)'P', candidate[1]);
        Assert.Equal((byte)'%', entrepreneur[0]);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeSalesCommercial : ISalesCommercialService
    {
        public Task<SalesCommercialSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SalesCommercialSettings());

        public Task<PartnerSalesCatalogDto> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PartnerSalesCatalogDto(
                25m, 2m, 1m, 7, 2m,
                [new VacancyTypeCostDto("Regular", "Regulier", 1m, 25m, true)],
                [new SalesPackageDto(Guid.NewGuid(), "Starter", "ST", "pack", 10, 250m, "Demo", true, 1)]));

        public Task<SalesCommercialAdminDto> GetAdminAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SalesCommercialSettings> UpdateSettingsAsync(
            decimal baseTokenValueEuro,
            decimal highlightCarouselTokens,
            decimal highlightPulseTokens,
            int highlightCarouselDays,
            decimal startHighlightBonusTokens,
            decimal? directCommissionRate = null,
            decimal? indirectCommissionRate = null,
            int? commissionDurationDays = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VacancyTypeTokenCost> UpdateVacancyTypeCostAsync(
            VacancyKind kind,
            decimal costTokens,
            bool isActive,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<decimal> GetPublishCostTokensAsync(VacancyKind kind, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<decimal> GetHighlightCostTokensAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> GetHighlightDaysAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SalesPackage> UpsertPackageAsync(SalesPackage package, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePackageAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Slogan", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(
            PlatformCompanyUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
