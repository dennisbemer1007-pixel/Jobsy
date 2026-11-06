namespace Jobsy.Core.Security;

/// <summary>Auth UI masking: first letter + four bullets + domain. Never log the result as if it were safe PII-free.</summary>
public static class EmailMask
{
    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
        {
            return "••••";
        }

        var local = trimmed[..at];
        var domain = trimmed[(at + 1)..];
        var first = local[0];
        return $"{first}••••@{domain}";
    }
}
