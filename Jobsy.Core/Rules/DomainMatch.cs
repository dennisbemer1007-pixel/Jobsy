namespace Jobsy.Core.Rules;

/// <summary>
/// Public-suffix-aware registrable-domain matching for business-e-mail verification (D14/D15).
/// </summary>
public static class DomainMatch
{
    /// <summary>
    /// Common multi-part public suffixes used in NL / EU (lightweight; not a full PSL).
    /// </summary>
    private static readonly string[] MultiPartSuffixes =
    [
        "co.uk", "org.uk", "ac.uk", "gov.uk",
        "com.au", "net.au", "org.au",
        "co.nz", "co.za",
        "com.br", "com.mx"
    ];

    public static string? ExtractEmailDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.Trim().LastIndexOf('@');
        if (at < 0 || at == email.Trim().Length - 1)
        {
            return null;
        }

        return email.Trim()[(at + 1)..].Trim().ToLowerInvariant();
    }

    public static string? ExtractHost(string? urlOrHost)
    {
        if (string.IsNullOrWhiteSpace(urlOrHost))
        {
            return null;
        }

        var raw = urlOrHost.Trim().ToLowerInvariant();
        if (!raw.Contains("://", StringComparison.Ordinal))
        {
            raw = "https://" + raw;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host.Trim('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return string.IsNullOrWhiteSpace(host) ? null : host;
    }

    public static string? RegistrableDomain(string? hostOrUrl)
    {
        var host = ExtractHost(hostOrUrl) ?? hostOrUrl?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        foreach (var suffix in MultiPartSuffixes)
        {
            if (host.Equals(suffix, StringComparison.Ordinal)
                || host.EndsWith("." + suffix, StringComparison.Ordinal))
            {
                var without = host[..^(suffix.Length + 1)];
                var label = without.Contains('.')
                    ? without[(without.LastIndexOf('.') + 1)..]
                    : without;
                return string.IsNullOrWhiteSpace(label) ? null : $"{label}.{suffix}";
            }
        }

        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return host;
        }

        return $"{parts[^2]}.{parts[^1]}";
    }

    /// <summary>
    /// True when the e-mail's registrable domain equals a website's, or is a subdomain of it.
    /// </summary>
    public static bool EmailMatchesWebsite(string? email, string? websiteUrlOrHost)
    {
        var emailDomain = ExtractEmailDomain(email);
        if (string.IsNullOrWhiteSpace(emailDomain) || FreeMailDomains.IsFreeMail(emailDomain))
        {
            return false;
        }

        var siteReg = RegistrableDomain(websiteUrlOrHost);
        var emailReg = RegistrableDomain(emailDomain);
        if (string.IsNullOrWhiteSpace(siteReg) || string.IsNullOrWhiteSpace(emailReg))
        {
            return false;
        }

        if (emailReg.Equals(siteReg, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Subdomain of the KVK website registrable domain (e.g. hr.groenenzorg.nl).
        return emailDomain.EndsWith("." + siteReg, StringComparison.OrdinalIgnoreCase);
    }

    public static bool EmailMatchesAnyWebsite(string? email, IEnumerable<string>? websites)
    {
        if (websites is null)
        {
            return false;
        }

        foreach (var site in websites)
        {
            if (EmailMatchesWebsite(email, site))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Equal registrable domain, or the e-mail domain is a subdomain of a website's registrable domain.
    /// Free-mail domains never match.
    /// </summary>
    public static bool Matches(string? emailDomain, IEnumerable<string>? kvkWebsites)
    {
        if (string.IsNullOrWhiteSpace(emailDomain) || kvkWebsites is null)
        {
            return false;
        }

        var domain = emailDomain.Trim().ToLowerInvariant();
        if (domain.Contains('@', StringComparison.Ordinal))
        {
            domain = ExtractEmailDomain(domain) ?? domain;
        }

        if (FreeMailDomains.IsFreeMail(domain))
        {
            return false;
        }

        var emailReg = RegistrableDomain(domain);
        if (string.IsNullOrWhiteSpace(emailReg))
        {
            return false;
        }

        foreach (var site in kvkWebsites)
        {
            var siteReg = RegistrableDomain(site);
            if (string.IsNullOrWhiteSpace(siteReg))
            {
                continue;
            }

            if (emailReg.Equals(siteReg, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (domain.EndsWith("." + siteReg, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
