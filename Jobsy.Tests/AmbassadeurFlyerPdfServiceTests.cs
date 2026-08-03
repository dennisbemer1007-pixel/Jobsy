using System.Text;
using System.Text.RegularExpressions;
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
    public async Task Render_Candidate_And_Entrepreneur_Flyers_Produce_Single_Page_Pdfs()
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
            new FakeSalesCommercial(packageCount: 8),
            new FakeCompanySettings(),
            new FakeFeatures());

        var candidate = await service.RenderAsync("AM-FLYER1", AmbassadeurFlyerKind.Candidate);
        var entrepreneur = await service.RenderAsync("AM-FLYER1", AmbassadeurFlyerKind.Entrepreneur);

        Assert.True(candidate.Length > 500);
        Assert.True(entrepreneur.Length > 500);
        Assert.Equal((byte)'%', candidate[0]);
        Assert.Equal((byte)'P', candidate[1]);
        Assert.Equal((byte)'%', entrepreneur[0]);
        Assert.Equal(1, PdfPageCounter.Count(candidate));
        Assert.Equal(1, PdfPageCounter.Count(entrepreneur));

        var candidateText = Encoding.Latin1.GetString(candidate);
        var entrepreneurText = Encoding.Latin1.GetString(entrepreneur);
        Assert.DoesNotContain("/werven/", candidateText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/register", entrepreneurText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lobsy.nl/werven", candidateText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lobsy.nl/register", entrepreneurText, StringComparison.OrdinalIgnoreCase);
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
        private readonly int _packageCount;

        public FakeSalesCommercial(int packageCount) => _packageCount = packageCount;

        public Task<SalesCommercialSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SalesCommercialSettings());

        public Task<PartnerSalesCatalogDto> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
        {
            var packages = Enumerable.Range(1, _packageCount)
                .Select(i => new SalesPackageDto(
                    Guid.NewGuid(),
                    $"Pakket {i}",
                    $"P{i}",
                    "Standard",
                    10 * i,
                    100m * i,
                    "Demo",
                    true,
                    i))
                .ToList();

            return Task.FromResult(new PartnerSalesCatalogDto(
                25m, 2m, 1m, 7, 2m,
                [
                    new VacancyTypeCostDto("Regular", "Regulier", 1m, 25m, true),
                    new VacancyTypeCostDto("Internship", "Stage", 0.5m, 12.5m, true),
                    new VacancyTypeCostDto("Volunteer", "Vrijwillig", 0m, 0m, true)
                ],
                packages));
        }

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

internal static class PdfPageCounter
{
    private static readonly Regex PageObject = new(
        @"/Type\s*/Page(?!\s*s)",
        RegexOptions.Compiled);

    public static int Count(byte[] pdf)
    {
        var text = Encoding.Latin1.GetString(pdf);
        return PageObject.Matches(text).Count;
    }
}
