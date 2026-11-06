using Jobsy.Core.Privacy;

namespace Jobsy.Web.Admin;

/// <summary>Display helpers for the Lobsy company form. Empty KvK, VAT and address stay empty.</summary>
public static class CompanyFormFields
{
    public const string IbanExample = "NL00KNAB0123456789";

    public static string DisplayCountry(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return "Nederland";
        }

        return stored.Trim() switch
        {
            "NL" or "NLD" or "Netherlands" or "The Netherlands" or "Nederland" => "Nederland",
            var other => other
        };
    }

    public static string CountryToStore(string? displayed)
    {
        if (string.IsNullOrWhiteSpace(displayed))
        {
            return "NL";
        }

        return displayed.Trim() switch
        {
            "Nederland" or "NL" or "NLD" or "Netherlands" or "The Netherlands" => "NL",
            var other => other
        };
    }

    public static string IbanPlaceholder(string? masked)
        => string.IsNullOrWhiteSpace(masked) || masked.Trim() == "—"
            ? IbanExample
            : masked.Trim();

    /// <summary>Empty, a dash or a mask means “do not change the stored IBAN”.</summary>
    public static string? IbanToSave(string? typed)
        => IbanMasking.IsFullIbanInput(typed) ? typed!.Trim() : null;
}
