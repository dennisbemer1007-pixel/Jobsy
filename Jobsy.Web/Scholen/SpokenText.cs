namespace Jobsy.Web.Scholen;

/// <summary>
/// Joins read-aloud parts so a heading and a sentence do not run together,
/// and a part that already ends with . ! ? or … does not gain a second mark.
/// </summary>
public static class SpokenText
{
    public static string Join(params string?[] parts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var text = part.Trim();
            if (sb.Length == 0)
            {
                sb.Append(text);
                continue;
            }

            var end = sb[^1];
            sb.Append(end is '.' or '!' or '?' or '…' ? ' ' : ". ");
            sb.Append(text);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Drops a leading copy of <paramref name="lead"/> from <paramref name="body"/>
    /// so a heading and the first sentence are not spoken twice.
    /// </summary>
    public static string WithoutRepeatedLead(string? lead, string? body)
    {
        var text = (body ?? "").Trim();
        var heading = NormalizeLead(lead);
        if (heading.Length == 0 || text.Length == 0)
        {
            return text;
        }

        if (!text.StartsWith(heading, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        return text[heading.Length..].TrimStart(' ', '.', '!', '?', '…', ':', '—', '-', '·').Trim();
    }

    private static string NormalizeLead(string? lead)
        => (lead ?? "").Trim().TrimEnd(' ', '.', '!', '?', '…', ':');
}
