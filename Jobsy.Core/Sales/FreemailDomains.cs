namespace Jobsy.Core.Sales;

/// <summary>
/// Consumer/freemail domains excluded from the self-referral e-mail-domain rule (D2).
/// </summary>
public static class FreemailDomains
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com",
        "googlemail.com",
        "hotmail.com",
        "hotmail.nl",
        "outlook.com",
        "outlook.nl",
        "live.com",
        "live.nl",
        "msn.com",
        "icloud.com",
        "me.com",
        "yahoo.com",
        "yahoo.nl",
        "ziggo.nl",
        "kpnmail.nl",
        "kpnplanet.nl",
        "xs4all.nl",
        "planet.nl",
        "home.nl",
        "hetnet.nl",
        "telfort.nl",
        "upcmail.nl",
        "casema.nl",
        "chello.nl",
        "proton.me",
        "protonmail.com"
    };

    public static bool IsFreemail(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        return Domains.Contains(domain.Trim());
    }

    public static string? TryGetDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.LastIndexOf('@');
        if (at < 0 || at >= email.Length - 1)
        {
            return null;
        }

        return email[(at + 1)..].Trim().ToLowerInvariant();
    }
}
