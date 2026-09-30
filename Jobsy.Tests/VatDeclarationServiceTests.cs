using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class VatDeclarationServiceTests
{
    [Fact]
    public async Task Generate_marks_token_and_sm_invoices_and_excludes_from_next_preview()
    {
        await using var db = CreateDb();
        SeedPlatform(db);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Klant BV",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        var smUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smUserId,
            Email = "sm@test.nl",
            FullName = "SM Test",
            Role = UserRole.SalesManager,
            IsActive = true
        });

        var checkoutId = Guid.NewGuid();
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(121.00m);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_vat1",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 121m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreditedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "LOB-TK-2026-0001",
            TokenPurchaseCheckoutId = checkoutId,
            CompanyId = companyId,
            MolliePaymentId = "stub_pay_vat1",
            PackSize = 10,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            CompanyName = "Klant BV",
            IssuedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        db.SelfBillingInvoices.Add(new SelfBillingInvoice
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = smUserId,
            InvoiceNumber = "SB-2026-0001",
            SalesManagerCompanyName = "SM BV",
            SalesManagerKvkNumber = "2",
            SalesManagerVatNumber = "NL2",
            SalesManagerAddress = "b",
            SubtotalExVat = 100m,
            VatAmount = 21m,
            TotalInclVat = 121m,
            VatRate = 0.21m,
            VatTreatment = SalesManagerVatTreatment.Standard21,
            Status = SelfBillingInvoiceStatus.Paid,
            CreatedAt = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            IssuedAt = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            PaidAt = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var sut = new VatDeclarationService(db, new PlatformCompanySettingsService(db));
        var preview = await sut.PreviewAsync(2026, 1);
        Assert.False(preview.AlreadyDeclared);
        Assert.Equal(1, preview.TokenInvoiceCount);
        Assert.Equal(1, preview.SalesManagerInvoiceCount);
        Assert.Equal(vat, preview.Rubriek1VatCents);
        Assert.Equal(2100, preview.Rubriek5VoorbelastingCents);
        Assert.Equal(vat - 2100, preview.AmountDueCents);

        var declaration = await sut.GenerateAndConfirmAsync(2026, 1, actorName: "Admin");
        Assert.Equal("2026-Q1", declaration.PeriodLabel);
        Assert.NotNull(declaration.PdfBytes);
        Assert.True(declaration.PdfBytes!.Length > 100);

        var token = await db.TokenPurchaseInvoices.SingleAsync();
        Assert.Equal(declaration.Id, token.VatDeclarationId);
        Assert.Equal("Verwerkt in aangifte 2026-Q1", token.VatDeclarationStatusLabel);

        var sm = await db.SelfBillingInvoices.SingleAsync();
        Assert.Equal(declaration.Id, sm.VatDeclarationId);
        Assert.Equal("Verwerkt in aangifte 2026-Q1", sm.VatDeclarationStatusLabel);

        var again = await sut.PreviewAsync(2026, 1);
        Assert.True(again.AlreadyDeclared);
        Assert.Equal(0, again.TokenInvoiceCount);
        Assert.Equal(0, again.SalesManagerInvoiceCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateAndConfirmAsync(2026, 1));
    }

    [Fact]
    public async Task Generate_allows_supplemental_when_new_open_items_appear()
    {
        await using var db = CreateDb();
        SeedPlatform(db);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Klant BV",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });

        var checkout1 = Guid.NewGuid();
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(121.00m);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkout1,
            PaymentId = "stub_pay_a",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 121m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "LOB-TK-2026-0001",
            TokenPurchaseCheckoutId = checkout1,
            CompanyId = companyId,
            MolliePaymentId = "stub_pay_a",
            PackSize = 10,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            CompanyName = "Klant BV",
            IssuedAt = new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var sut = new VatDeclarationService(db, new PlatformCompanySettingsService(db));
        var first = await sut.GenerateAndConfirmAsync(2026, 1);
        Assert.Equal("2026-Q1", first.PeriodLabel);

        var checkout2 = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkout2,
            PaymentId = "stub_pay_b",
            CompanyId = companyId,
            PackSize = 5,
            AmountEuro = 60.50m,
            AmountExVatCents = 5000,
            VatAmountCents = 1050,
            TotalAmountCents = 6050,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "LOB-TK-2026-0002",
            TokenPurchaseCheckoutId = checkout2,
            CompanyId = companyId,
            MolliePaymentId = "stub_pay_b",
            PackSize = 5,
            AmountExVatCents = 5000,
            VatAmountCents = 1050,
            TotalAmountCents = 6050,
            CompanyName = "Klant BV",
            IssuedAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var preview = await sut.PreviewAsync(2026, 1);
        Assert.False(preview.AlreadyDeclared);
        Assert.Equal(1, preview.TokenInvoiceCount);

        var second = await sut.GenerateAndConfirmAsync(2026, 1);
        Assert.Equal("2026-Q1-2", second.PeriodLabel);
        Assert.Equal(1, second.TokenInvoiceCount);

        var closed = await sut.PreviewAsync(2026, 1);
        Assert.True(closed.AlreadyDeclared);
        Assert.Equal(0, closed.TokenInvoiceCount);
    }

    [Fact]
    public async Task Preview_includes_consumer_kandidaat_aankopen_in_rubriek1()
    {
        await using var db = CreateDb();
        SeedPlatform(db);

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "k@test.nl",
            FullName = "K",
            Role = UserRole.Candidate,
            IsActive = true
        });
        var checkoutId = Guid.NewGuid();
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(2.99m);
        db.DeepAnalysisCheckouts.Add(new DeepAnalysisCheckout
        {
            Id = checkoutId,
            UserId = userId,
            Kind = AssessmentKind.Competence,
            PaymentId = "tr_c1",
            AmountEuro = 2.99m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = DeepAnalysisCheckoutStatus.Paid,
            PaidAtUtc = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            WaiverAcceptedAtUtc = DateTime.UtcNow,
            WaiverTextVersion = "2026-09"
        });
        db.ConsumerPurchaseInvoices.Add(new ConsumerPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "LOB-KT-2026-0001",
            DeepAnalysisCheckoutId = checkoutId,
            UserId = userId,
            CustomerName = "K",
            CustomerEmail = "k@test.nl",
            Description = "Uitgebreide test",
            Kind = AssessmentKind.Competence,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            MolliePaymentId = "tr_c1",
            IssuedAt = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var sut = new VatDeclarationService(db, new PlatformCompanySettingsService(db));
        var preview = await sut.PreviewAsync(2026, 1);
        Assert.Equal(1, preview.ConsumerInvoiceCount);
        Assert.Equal(ex, preview.ConsumerOmzetExVatCents);
        Assert.Equal(vat, preview.ConsumerVatCents);
        Assert.Equal(vat, preview.Rubriek1VatCents);

        var declaration = await sut.GenerateAndConfirmAsync(2026, 1);
        var inv = await db.ConsumerPurchaseInvoices.SingleAsync();
        Assert.Equal(declaration.Id, inv.VatDeclarationId);
        Assert.Contains("Verwerkt in aangifte", inv.VatDeclarationStatusLabel);
    }

    private static void SeedPlatform(JobsyDbContext db)
    {
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Bemer IT Solutions",
            KvkNumber = "12345678",
            VatNumber = "NL001234567B01",
            Address = "Teststraat 1",
            PostalCode = "2500AA",
            City = "Den Haag"
        });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
