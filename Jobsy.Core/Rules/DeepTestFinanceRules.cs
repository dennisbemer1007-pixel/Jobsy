using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// A flagged test account unlocks the uitgebreide analyse at zero euros.
/// That row is not revenue and must not enter VAT or finance totals.
/// </summary>
public static class DeepTestFinanceRules
{
    public const string TestUnlockMethod = "test";

    /// <summary>Payment mode and stored method when an admin made every candidate test free.</summary>
    public const string FreeForEveryoneMethod = "gratis";

    public static bool IsTestUnlock(string? paymentMethod)
        => string.Equals(paymentMethod, TestUnlockMethod, StringComparison.OrdinalIgnoreCase);

    public static bool IsFreeForEveryoneUnlock(string? paymentMethod)
        => string.Equals(paymentMethod, FreeForEveryoneMethod, StringComparison.OrdinalIgnoreCase);

    /// <summary>True for a test-account unlock and for the admin “tests gratis voor iedereen” mode.</summary>
    public static bool SkipsCandidatePayment(string? modeOrMethod)
        => IsTestUnlock(modeOrMethod) || IsFreeForEveryoneUnlock(modeOrMethod);

    public static bool IsZeroEuroUnlock(string? paymentMethod)
        => SkipsCandidatePayment(paymentMethod);

    public static IQueryable<ConsumerPurchaseInvoice> ExcludingTestUnlocks(
        this IQueryable<ConsumerPurchaseInvoice> query)
        => query.Where(i =>
            i.PaymentMethod == null
            || (i.PaymentMethod != TestUnlockMethod && i.PaymentMethod != FreeForEveryoneMethod));
}
