using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Strips direct identifiers from CV plain text before any LLM call.</summary>
public static partial class CvExtractionRedaction
{
    public const string NameToken = "«naam»";
    public const string EmailToken = "«e-mail»";
    public const string PhoneToken = "«telefoon»";
    public const string AddressToken = "«adres»";
    public const string BsnToken = "«bsn»";
    public const string BirthDateToken = "«geboortedatum»";

    public static string Redact(string cvText, string? knownFirstName = null, string? knownLastName = null)
    {
        if (string.IsNullOrWhiteSpace(cvText))
        {
            return string.Empty;
        }

        var redacted = cvText;
        redacted = EmailRegex().Replace(redacted, EmailToken);
        redacted = PhoneRegex().Replace(redacted, PhoneToken);
        redacted = BsnRegex().Replace(redacted, BsnToken);
        redacted = BirthDateRegex().Replace(redacted, BirthDateToken);
        redacted = PostalLineRegex().Replace(redacted, AddressToken);
        redacted = RedactKnownName(redacted, knownFirstName);
        redacted = RedactKnownName(redacted, knownLastName);
        redacted = RedactLikelyHeaderName(redacted);
        return redacted;
    }

    private static string RedactKnownName(string text, string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            return text;
        }

        var pattern = Regex.Escape(name.Trim());
        return Regex.Replace(text, pattern, NameToken, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string RedactLikelyHeaderName(string text)
    {
        using var reader = new StringReader(text);
        for (var i = 0; i < 6; i++)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            var trimmed = line.Trim();
            if (trimmed.Length is < 4 or > 80 || trimmed.Contains('@') || PhoneRegex().IsMatch(trimmed))
            {
                continue;
            }

            var wordCount = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount is >= 2 and <= 5 && !trimmed.Any(char.IsDigit))
            {
                return text.Replace(line, NameToken, StringComparison.Ordinal);
            }
        }

        return text;
    }

    [GeneratedRegex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:\+?\d[\d\s().\-]{7,}\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{9}\b", RegexOptions.CultureInvariant)]
    private static partial Regex BsnRegex();

    [GeneratedRegex(@"\b(?:geboren|op)?\s*\d{1,2}[\-/.\s]\d{1,2}[\-/.\s]\d{2,4}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BirthDateRegex();

    [GeneratedRegex(@"(?m)^[^\n]{0,60}\b\d{4}\s?[A-Za-z]{2}\b[^\n]{0,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex PostalLineRegex();
}