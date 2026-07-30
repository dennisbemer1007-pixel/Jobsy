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
            Location = new GeoPoint(51.9, 4.2)
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
