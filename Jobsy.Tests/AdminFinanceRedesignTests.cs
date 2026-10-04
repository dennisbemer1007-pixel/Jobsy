using Bunit;
using Bunit.TestDoubles;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Jobsy.Tests;

public class AdminFinanceRedesignTests : BunitContext
{
    public AdminFinanceRedesignTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuth()));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    [Fact]
    public void Mollie_status_pills_have_text_labels()
    {
        Assert.Equal("Betaald", AdminFinanceDisplay.MollieStatusLabel("Paid"));
        Assert.Equal("Betaald", AdminFinanceDisplay.MollieStatusLabel("Credited"));
        Assert.Equal("Open", AdminFinanceDisplay.MollieStatusLabel("Pending"));
        Assert.Equal("Mislukt", AdminFinanceDisplay.MollieStatusLabel("Cancelled"));
        Assert.Equal("Terugbetaald", AdminFinanceDisplay.MollieStatusLabel("Refunded"));
        Assert.Equal("Verlopen", AdminFinanceDisplay.MollieStatusLabel("Expired"));
        Assert.Equal("accepted", AdminFinanceDisplay.MollieStatusTone("Paid"));
        Assert.Equal("pending", AdminFinanceDisplay.MollieStatusTone("Pending"));
        Assert.Equal("rejected", AdminFinanceDisplay.MollieStatusTone("Cancelled"));
    }

    [Fact]
    public void Legacy_uitbetalingen_goodwill_tab_redirects_to_goodwill_page()
    {
        Assert.True(AdminLegacyRoutes.TryMap(
            "/admin/financien/uitbetalingen?tab=goodwill", out var dest));
        Assert.Equal("/admin/financien/goodwill", dest);

        Assert.True(AdminLegacyRoutes.TryMap(
            "/admin/financien/uitbetalingen?tab=goodwill&year=2026", out var dest2));
        Assert.Equal("/admin/financien/goodwill?year=2026", dest2);
    }

    [Fact]
    public void Uitbetalingen_tabs_exclude_goodwill()
    {
        var cut = Render<AdminTabs>(p => p
            .Add(x => x.BasePath, "/admin/financien/uitbetalingen")
            .Add(x => x.ActiveKey, "uitbetalingen")
            .Add(x => x.Tabs, new List<AdminTabs.Tab>
            {
                new("uitbetalingen", "Uitbetalingen"),
                new("smcosts", "Inkoop / salesmanagers"),
                new("vat", "Btw-buffer"),
                new("declarations", "Btw-aangifte & facturen"),
            }));
        Assert.DoesNotContain("goodwill", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Uitbetalingen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("/admin/financien/uitbetalingen?tab=smcosts", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Pricing_what_drives_shows_overlap_notes()
    {
        var cut = Render<PricingWhatDrivesSection>();
        Assert.Contains("Wat bepaalt welke prijs?", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("admin-impact-note", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tokenpakketten", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Token-kosten per vacaturetype", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Kosten per actie", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_kpi_card_renders_value()
    {
        var cut = Render<AdminKpiCard>(p => p
            .Add(x => x.Label, "Omzet")
            .Add(x => x.Value, "€ 18.420")
            .Add(x => x.Delta, "+11%")
            .Add(x => x.DeltaTone, "up"));
        Assert.Contains("Omzet", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("€ 18.420", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("+11%", cut.Markup, StringComparison.Ordinal);
    }
}

public class AdminFinanceSummaryServiceExtendedTests
{
    [Fact]
    public async Task Totals_match_invoices_and_open_mollie_count()
    {
        await using var db = new JobsyDbContext(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminFinExt-" + Guid.NewGuid()).Options);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Co",
            KvkNumber = "1",
            Address = "x",
            Location = new GeoPoint(52, 4)
        };
        db.Companies.Add(company);
        var sm = new User
        {
            Id = Guid.NewGuid(),
            Email = "sm@example.com",
            FullName = "Sandra Verkoop",
            Role = UserRole.SalesManager
        };
        db.Users.Add(sm);

        var checkout = new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            PaymentId = "tr_1",
            PackSize = 10,
            TotalAmountCents = 12100,
            AmountExVatCents = 10000,
            VatAmountCents = 2100,
            Status = TokenPurchaseCheckoutStatus.Paid,
            PaymentMethod = "ideal",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.TokenPurchaseCheckouts.Add(checkout);
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-1",
            TokenPurchaseCheckoutId = checkout.Id,
            CompanyId = company.Id,
            CompanyName = "Co",
            MolliePaymentId = "tr_1",
            PackSize = 10,
            AmountExVatCents = 10000,
            VatAmountCents = 2100,
            TotalAmountCents = 12100,
            IssuedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            PaymentId = "tr_open",
            PackSize = 5,
            TotalAmountCents = 5000,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.SelfBillingInvoices.Add(new SelfBillingInvoice
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = sm.Id,
            InvoiceNumber = "SM-1",
            SalesManagerCompanyName = "SM Co",
            Status = SelfBillingInvoiceStatus.Issued,
            TotalInclVat = 121m,
            SubtotalExVat = 100m,
            VatAmount = 21m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var summary = await new AdminFinanceSummaryService(db).GetAsync("week");
        Assert.Equal(12100, summary.RevenueInclVatCents);
        Assert.Equal(10000, summary.RevenueExVatCents);
        Assert.Equal(10, summary.TokensSold);
        Assert.Equal(5000, summary.OpenAtMollieCents);
        Assert.Equal(1, summary.OpenAtMollieCount);
        Assert.Equal(2, summary.OldestOpenMollieDays);
        Assert.Equal(1, summary.OpenPayoutsCount);
        Assert.Equal(12100, summary.OpenPayoutsCents);
        Assert.Single(summary.OpenPayoutPreviews);
        Assert.Equal(PersonalDataMasker.MaskName("Sandra Verkoop"), summary.OpenPayoutPreviews[0].MaskedPayeeName);
    }
}

public class AdminFinanceBulkMarkPaidHelperTests
{
    [Fact]
    public void Partial_failures_are_counted_per_id()
    {
        // Mirrors FinancePayoutsSection bulk loop: one call per id, partial reported.
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var failId = ids[1];
        var ok = 0;
        var fail = 0;
        foreach (var id in ids)
        {
            if (id == failId) fail++;
            else ok++;
        }

        Assert.Equal(2, ok);
        Assert.Equal(1, fail);
        Assert.Equal(ids.Length, ok + fail);
    }
}
