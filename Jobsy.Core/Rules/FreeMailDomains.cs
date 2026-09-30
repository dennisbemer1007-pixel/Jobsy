namespace Jobsy.Core.Rules;

/// <summary>
/// Consumer / disposable e-mail domains that cannot prove company ownership via domain match.
/// </summary>
public static class FreeMailDomains
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "outlook.nl", "hotmail.com", "hotmail.nl", "hotmail.be",
        "live.com", "live.nl", "msn.com", "passport.com",
        "icloud.com", "me.com", "mac.com",
        "yahoo.com", "yahoo.nl", "ymail.com",
        "ziggo.nl", "kpnmail.nl", "kpnplanet.nl", "planet.nl", "home.nl",
        "hetnet.nl", "casema.nl", "chello.nl", "xs4all.nl", "telfort.nl",
        "upcmail.nl", "tele2.nl", "online.nl",
        "proton.me", "protonmail.com", "pm.me",
        "gmx.com", "gmx.de", "gmx.net", "web.de", "mail.com", "aol.com",
        "yandex.com", "yandex.ru",
        "mailinator.com", "guerrillamail.com", "tempmail.com", "10minutemail.com",
        "yopmail.com", "trashmail.com", "discard.email", "temp-mail.org",
        "sharklasers.com", "guerrillamailblock.com", "grr.la", "guerrillamail.info",
        "pokemail.net", "spam4.me", "bccto.me", "dispostable.com", "mailnesia.com",
        "maildrop.cc", "getnada.com", "emailondeck.com", "throwaway.email"
    };

    public static bool IsFreeMail(string? domainOrEmail)
    {
        if (string.IsNullOrWhiteSpace(domainOrEmail))
        {
            return false;
        }

        var value = domainOrEmail.Trim().ToLowerInvariant();
        var at = value.LastIndexOf('@');
        var domain = at >= 0 ? value[(at + 1)..] : value;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        return Domains.Contains(domain);
    }
}
