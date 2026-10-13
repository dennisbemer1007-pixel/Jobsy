using Jobsy.Core.Options;

namespace Jobsy.Core.Email;

/// <summary>From / Reply-To resolution shared by transport and admin UI (no MIME dependency).</summary>
public static class MailAddressResolution
{
    public static string ResolveFromAddress(string? dbFrom, MailOptions? options = null)
    {
        var candidate = FirstNonEmpty(dbFrom, options?.FromAddress, MailOptions.DefaultFromAddress)
                        ?? MailOptions.DefaultFromAddress;
        return EnsureLobsyDisplayName(candidate);
    }

    public static string EnsureLobsyDisplayName(string fromAddress)
    {
        var trimmed = fromAddress.Trim();
        // "name <addr>" or bare addr
        var lt = trimmed.IndexOf('<');
        var gt = trimmed.LastIndexOf('>');
        if (lt >= 0 && gt > lt)
        {
            var name = trimmed[..lt].Trim().Trim('"');
            var addr = trimmed[(lt + 1)..gt].Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return $"Lobsy <{addr}>";
            }

            return $"{name} <{addr}>";
        }

        return $"Lobsy <{trimmed}>";
    }

    public static bool IsFromDomainMismatch(string fromAddress)
    {
        var addr = ExtractAddress(fromAddress);
        var at = addr.LastIndexOf('@');
        if (at < 0)
        {
            return true;
        }

        var domain = addr[(at + 1)..];
        return !string.Equals(domain, "mail.lobsy.nl", StringComparison.OrdinalIgnoreCase);
    }

    public static string EffectiveReplyTo(MailOptions? options)
        => FirstNonEmpty(options?.ReplyTo, MailOptions.DefaultSupportAddress)
           ?? MailOptions.DefaultSupportAddress;

    public static string ExtractAddress(string fromAddress)
    {
        var trimmed = fromAddress.Trim();
        var lt = trimmed.IndexOf('<');
        var gt = trimmed.LastIndexOf('>');
        if (lt >= 0 && gt > lt)
        {
            return trimmed[(lt + 1)..gt].Trim();
        }

        return trimmed;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }
}
