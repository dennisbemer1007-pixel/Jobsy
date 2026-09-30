using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Core.Security;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Sales;

public class SalesFoundationUnitTests
{
    [Fact]
    public void MfaPolicy_requires_SalesManager_and_Ambassadeur()
    {
        Assert.True(MfaPolicy.IsRequired(UserRole.SalesManager));
        Assert.True(MfaPolicy.IsRequired(UserRole.Ambassadeur));
    }

    [Theory]
    [InlineData(2026, 4, 27)] // Koningsdag
    [InlineData(2026, 4, 6)]  // Easter Monday 2026
    [InlineData(2026, 5, 14)] // Ascension 2026
    [InlineData(2026, 5, 25)] // Whit Monday 2026
    [InlineData(2026, 12, 25)]
    [InlineData(2026, 12, 26)]
    [InlineData(2026, 1, 1)]
    public void DutchHolidays_2026(int y, int m, int d)
        => Assert.True(DutchHolidays.IsPublicHoliday(new DateOnly(y, m, d)));

    [Fact]
    public void FirstWorkdayOfMonth_skips_weekend_and_new_year()
    {
        Assert.Equal(new DateOnly(2027, 1, 4), SalesClock.FirstWorkdayOfMonth(2027, 1));
        Assert.Equal(new DateOnly(2026, 10, 1), SalesClock.FirstWorkdayOfMonth(2026, 10));
    }

    [Fact]
    public void SalesMoney_formats_nl()
    {
        Assert.Equal("€\u00A01.284,50", SalesMoney.FormatPlain(1284.50m));
        Assert.Equal("+ €\u00A043,75", SalesMoney.FormatSigned(43.75m));
        Assert.Equal("– €\u00A0900,00", SalesMoney.FormatSigned(-900m));
        Assert.Contains("excl. btw", SalesMoney.Format(10m, SalesMoneyKind.ExVat), StringComparison.Ordinal);
    }

    [Fact]
    public void SalesLabels_cover_all_enum_values()
    {
        foreach (var type in SalesLabels.LabeledEnumTypes())
        {
            foreach (var value in Enum.GetValues(type))
            {
                var key = type.Name switch
                {
                    nameof(CommissionEntryKind) => SalesLabels.Key((CommissionEntryKind)value),
                    nameof(SalesPayoutRequestStatus) => SalesLabels.Key((SalesPayoutRequestStatus)value),
                    nameof(SalesPayoutRunStatus) => SalesLabels.Key((SalesPayoutRunStatus)value),
                    nameof(SelfBillingInvoiceStatus) => SalesLabels.Key((SelfBillingInvoiceStatus)value),
                    nameof(SalesManagerVatTreatment) => SalesLabels.Key((SalesManagerVatTreatment)value),
                    nameof(SalesAttributionSource) => SalesLabels.Key((SalesAttributionSource)value),
                    nameof(SalesManagerApplicationStatus) => SalesLabels.Key((SalesManagerApplicationStatus)value),
                    _ => throw new InvalidOperationException(type.Name)
                };
                Assert.False(string.IsNullOrWhiteSpace(key), $"{type.Name}.{value}");
            }
        }
    }

    [Fact]
    public void SalesLegacyRoutes_cover_all_rows()
    {
        Assert.Equal("/sales", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager"]);
        Assert.Equal("/sales/link", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager/toolkit"]);
        Assert.Equal("/sales/aanbevelen", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager/referrals"]);
        Assert.Equal("/sales/start", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager/onboarding"]);
        Assert.Equal("/sales/wallet?tab=facturen", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager/invoices"]);
        Assert.Equal("/sales/wallet/uitbetalen", Jobsy.Web.Navigation.SalesLegacyRoutes.Map["/salesmanager/payout-checkout"]);
    }

    [Fact]
    public void SalesNav_hides_unavailable_and_recruit()
    {
        var withRecruit = SalesNav.VisibleItems(canRecruit: true).Select(i => i.Key).ToHashSet();
        Assert.Contains("recommend", withRecruit);
        Assert.DoesNotContain("employers", withRecruit); // IsAvailable false until 04
        Assert.DoesNotContain("profile", withRecruit);

        var without = SalesNav.VisibleItems(canRecruit: false).Select(i => i.Key).ToHashSet();
        Assert.DoesNotContain("recommend", without);
        Assert.Equal(5, SalesNav.BottomNavItems(true).Count + 0); // 4 bottom + Meer is UI
        Assert.Equal(4, SalesNav.BottomNavItems(true).Count);
    }
}
