using Jobsy.Core.Rules;

namespace Jobsy.Web.Admin;

/// <summary>Display helpers for finance tables (labels only — no amount/VAT logic).</summary>
public static class AdminFinanceDisplay
{
    public static string MollieStatusLabel(string? status) => status switch
    {
        "Paid" or "Credited" => "Betaald",
        "Pending" => "Open",
        "Cancelled" => "Mislukt",
        "Refunded" => "Terugbetaald",
        "Expired" => "Verlopen",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string MollieStatusTone(string? status) => status switch
    {
        "Paid" or "Credited" => "accepted",
        "Pending" => "pending",
        "Cancelled" or "Expired" => "rejected",
        "Refunded" => "neutral",
        _ => "neutral"
    };

    public static string PaymentMethodLabel(string? method)
        => MolliePaymentMethods.DisplayName(method);

    public static string Euro(int cents)
        => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));

    public static string Euro(decimal amount)
        => amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));

    /// <summary>NL quarterly VAT filing deadline: last day of the month after the quarter.</summary>
    public static DateOnly VatDueDate(int year, int quarter)
    {
        var monthAfterQuarter = quarter * 3 + 1;
        var dueYear = year;
        if (monthAfterQuarter > 12)
        {
            monthAfterQuarter = 1;
            dueYear++;
        }

        var lastDay = DateTime.DaysInMonth(dueYear, monthAfterQuarter);
        return new DateOnly(dueYear, monthAfterQuarter, lastDay);
    }
}
