using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class EngagementCatalogTests
{
    [Fact]
    public void Catalog_has_six_stable_ids_and_driver_links()
    {
        Assert.Equal(6, EngagementCatalog.All.Length);
        Assert.All(EngagementCatalog.All, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.Id));
            Assert.False(string.IsNullOrWhiteSpace(i.TitleKey));
            Assert.StartsWith("WaEngage.", i.TitleKey, StringComparison.Ordinal);
            Assert.True(i.DriverCode is SchwartzValuesCatalog.Impact or SchwartzValuesCatalog.Connection);
        });
        Assert.True(EngagementCatalog.TryGet("duurzaam", out var d));
        Assert.Equal(SchwartzValuesCatalog.Impact, d.DriverCode);
        Assert.True(EngagementCatalog.TryGet("leerbedrijf", out var l));
        Assert.Equal(SchwartzValuesCatalog.Connection, l.DriverCode);
    }
}

public class EngagementBonusTests
{
    [Fact]
    public void Bonus_requires_waardentest_and_driver_threshold()
    {
        var claims = new[]
        {
            new CompanyEngagementMatchItem(EngagementCatalog.Duurzaam, CompanyEngagementStatuses.SelfDeclared),
            new CompanyEngagementMatchItem(EngagementCatalog.Lokaal, CompanyEngagementStatuses.Checked)
        };

        Assert.Equal(0, EngagementCatalog.ComputeBonus(null, claims));
        Assert.Equal(0, EngagementCatalog.ComputeBonus(Values(impact: 69, connection: 69), claims));
        Assert.Equal(1, EngagementCatalog.ComputeBonus(Values(impact: 70, connection: 10), claims));
        Assert.Equal(2, EngagementCatalog.ComputeBonus(Values(impact: 10, connection: 70), claims));
        Assert.Equal(3, EngagementCatalog.ComputeBonus(Values(impact: 70, connection: 70), claims));
    }

    [Fact]
    public void Bonus_caps_at_five_and_never_negative()
    {
        var claims = EngagementCatalog.All
            .Select(i => new CompanyEngagementMatchItem(i.Id, CompanyEngagementStatuses.Checked))
            .ToList();
        var bonus = EngagementCatalog.ComputeBonus(Values(impact: 90, connection: 90), claims);
        Assert.Equal(5, bonus);
        Assert.True(bonus >= 0);
    }

    [Fact]
    public void Calculator_applies_bonus_with_explanation_and_caps_at_five()
    {
        var input = MakeInput(
            Values(impact: 85, connection: 40),
            [
                new CompanyEngagementMatchItem(EngagementCatalog.Duurzaam, CompanyEngagementStatuses.SelfDeclared),
                new CompanyEngagementMatchItem(EngagementCatalog.WerkVoorIedereen, CompanyEngagementStatuses.Checked)
            ],
            "Groen & Zorg");
        var without = ProfileVacancyMatchCalculator.Calculate(MakeInput(Values(impact: 85, connection: 40), [], "Groen & Zorg"));
        var match = ProfileVacancyMatchCalculator.Calculate(input);
        Assert.Equal(3, match.EngagementBonus);
        Assert.Equal(without.TotalPercent + 3, match.TotalPercent);
        Assert.Contains(match.Why, w => w.Kind == "engagement" && w.Text.Contains("Bonus +3", StringComparison.Ordinal));
        Assert.True(match.TotalPercent <= 100);
        Assert.Equal("company-engagement-v1", CandidateInsightsFingerprint.MatchAlgorithmVersion);
    }

    [Fact]
    public void Calculator_no_waardentest_means_zero_bonus()
    {
        var match = ProfileVacancyMatchCalculator.Calculate(MakeInput(
            null,
            [new CompanyEngagementMatchItem(EngagementCatalog.Duurzaam, CompanyEngagementStatuses.Checked)],
            "X"));
        Assert.Equal(0, match.EngagementBonus);
        Assert.DoesNotContain(match.Why, w => w.Kind == "engagement");
    }

    [Fact]
    public void Calculator_total_cap_100_with_large_bonus()
    {
        var baseMatch = ProfileVacancyMatchCalculator.Calculate(MakeInput(Values(impact: 95, connection: 95), [], "X"));
        var claims = EngagementCatalog.All
            .Select(i => new CompanyEngagementMatchItem(i.Id, CompanyEngagementStatuses.Checked))
            .ToList();
        var withBonus = ProfileVacancyMatchCalculator.Calculate(MakeInput(Values(impact: 95, connection: 95), claims, "X"));
        Assert.Equal(5, withBonus.EngagementBonus);
        Assert.Equal(Math.Min(100, baseMatch.TotalPercent + 5), withBonus.TotalPercent);
    }

    private static ProfileVacancyMatchInput MakeInput(
        SchwartzValuesScores? values,
        IReadOnlyList<CompanyEngagementMatchItem> claims,
        string company)
        => new()
        {
            VacancyId = Guid.NewGuid(),
            VacancyTitle = "Zorgmedewerker",
            VacancyDescription = "Zorg en welzijn",
            Core = new MatchScoreInput
            {
                EstimatedTravelMinutes = 10,
                MaxTravelMinutes = 45,
                CandidateHours = new HoursRange(16, 24),
                VacancyHours = new HoursRange(16, 24),
                CandidateAgeYears = 30
            },
            WorkTypes = [WorkTypeLabels.Zorg],
            CandidateValuesScores = values,
            CompanyEngagement = claims,
            CompanyName = company
        };

    private static SchwartzValuesScores Values(int impact, int connection)
        => new(Autonomy: 50, Connection: connection, Achievement: 50, Stability: 50, Impact: impact);
}

public class CompanyEngagementServiceTests
{
    [Fact]
    public async Task Save_validates_https_lengths_unique_and_resets_checked_on_proof_change()
    {
        await using var db = CreateDb();
        var company = Seed(db);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyEngagementUpdate([
                new CompanyEngagementClaimInput(EngagementCatalog.Duurzaam, ProofUrl: "http://insecure.example")
            ])));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyEngagementUpdate([
                new CompanyEngagementClaimInput(EngagementCatalog.Duurzaam, ProofText: new string('x', 301))
            ])));

        var saved = await sut.SaveAsync(
            company.Id,
            new CompanyEngagementUpdate([
                new CompanyEngagementClaimInput(EngagementCatalog.Duurzaam, ProofUrl: "https://example.com/a")
            ]));
        Assert.Single(saved.Claims);
        var claimId = (await db.CompanyEngagementClaims.SingleAsync()).Id;
        await sut.CheckAsync(claimId, Guid.NewGuid());

        var afterProof = await sut.SaveAsync(
            company.Id,
            new CompanyEngagementUpdate([
                new CompanyEngagementClaimInput(EngagementCatalog.Duurzaam, ProofUrl: "https://example.com/b")
            ]));
        Assert.Equal(CompanyEngagementStatuses.SelfDeclared, afterProof.Claims[0].Status);
    }

    [Fact]
    public async Task Removed_claim_cannot_be_readded_for_30_days()
    {
        await using var db = CreateDb();
        var company = Seed(db);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await sut.SaveAsync(company.Id, new CompanyEngagementUpdate([
            new CompanyEngagementClaimInput(EngagementCatalog.Lokaal, ProofText: "Club")
        ]));
        var claim = await db.CompanyEngagementClaims.SingleAsync();
        await sut.RemoveAsync(claim.Id, Guid.NewGuid(), "Onjuist");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SaveAsync(
            company.Id,
            new CompanyEngagementUpdate([
                new CompanyEngagementClaimInput(EngagementCatalog.Lokaal, ProofText: "Club")
            ])));
        Assert.Contains("verwijderd", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Moderation_check_remove_reset_are_audited_and_report_lands_in_queue()
    {
        await using var db = CreateDb();
        var company = Seed(db);
        company.VerificationStatus = CompanyVerificationStatus.Verified;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await sut.SaveAsync(company.Id, new CompanyEngagementUpdate([
            new CompanyEngagementClaimInput(EngagementCatalog.Diversiteit, ProofUrl: "https://example.com/d")
        ]));
        var claim = await db.CompanyEngagementClaims.SingleAsync();
        var admin = Guid.NewGuid();

        await sut.CheckAsync(claim.Id, admin);
        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Category == "admin.engagement.check"));

        await sut.ReportAsync(company.Id, EngagementCatalog.Diversiteit, "Klopt niet echt", "a@b.nl");
        var reports = await sut.ListAdminQueueAsync("reports", null);
        Assert.Contains(reports, r => r.ClaimId == claim.Id && r.OpenReportCount >= 1);

        await sut.ResetToSelfDeclaredAsync(claim.Id, admin);
        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Category == "admin.engagement.reset"));

        await sut.RemoveAsync(claim.Id, admin, "Misleidend");
        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Category == "admin.engagement.remove"));
        var removed = await db.CompanyEngagementClaims.AsNoTracking().SingleAsync();
        Assert.Equal(CompanyEngagementStatuses.Removed, removed.Status);
        Assert.Equal("Misleidend", removed.RemovedReason);
    }

    [Fact]
    public async Task Vestiging_reads_org_claims_and_public_hidden_when_unverified()
    {
        await using var db = CreateDb();
        var org = Seed(db, "Org");
        org.VerificationStatus = CompanyVerificationStatus.Verified;
        var child = Seed(db, "Child", parentId: org.Id);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await sut.SaveAsync(child.Id, new CompanyEngagementUpdate([
            new CompanyEngagementClaimInput(EngagementCatalog.EerlijkLoon, ProofText: "Cao VVT")
        ]));
        Assert.Equal(org.Id, (await db.CompanyEngagementClaims.SingleAsync()).CompanyId);
        var viaChild = await sut.GetAsync(child.Id);
        Assert.NotNull(viaChild);
        Assert.Single(viaChild!.Claims);

        org.VerificationStatus = CompanyVerificationStatus.Unverified;
        await db.SaveChangesAsync();
        Assert.False(PublicVisibility.IsCompanyPublic(org));
    }

    private static CompanyEngagementService CreateSut(JobsyDbContext db)
        => new(
            db,
            new MemoryCache(new MemoryCacheOptions()),
            new StubEmail(),
            NullLogger<CompanyEngagementService>.Instance);

    private static Company Seed(JobsyDbContext db, string name = "Co", Guid? parentId = null)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "90123456",
            Address = "A",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            ParentCompanyId = parentId,
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("engage-" + Guid.NewGuid())
            .Options);

    private sealed class StubEmail : IEmailService
    {
        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailDeliveryResult.Stub);
    }
}
