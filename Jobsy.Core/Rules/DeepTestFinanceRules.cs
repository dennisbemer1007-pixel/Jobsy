using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// A flagged test account unlocks the uitgebreide analyse at zero euros.
/// That row is not revenue and must not enter VAT or finance totals.
/// </summary>
public static class DeepTestFinanceRules
{
    public const string TestUnlockMethod = "test";

    public static bool IsTestUnlock(string? paymentMethod)
        => string.Equals(paymentMethod, TestUnlockMethod, StringComparison.OrdinalIgnoreCase);

    public static IQueryable<ConsumerPurchaseInvoice> ExcludingTestUnlocks(
        this IQueryable<ConsumerPurchaseInvoice> query)
        => query.Where(i => i.PaymentMethod == null || i.PaymentMethod != TestUnlockMethod);
}
