using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Jobsy.Core.Options;
using Jobsy.Core.Security;

namespace Jobsy.Core.Email;

/// <summary>
/// Acceptatie gate. An empty <see cref="MailOptions.AllowedRecipientPattern"/> means every recipient
/// (production). A pattern blocks everyone else, except exact addresses in
/// <see cref="MailOptions.AllowedRecipientAddresses"/>.
/// </summary>
public static class MailRecipientAllowList
{
    private static readonly ConcurrentDictionary<string, Regex?> Patterns = new(StringComparer.Ordinal);
    private static int _invalidPatternWarned;

    public readonly record struct Decision(bool Blocked, string MaskedAddress, bool PatternInvalid);

    public static Decision Evaluate(string? recipient, MailOptions? options)
    {
        var address = MailAddressResolution.ExtractAddress(recipient ?? string.Empty);
        var masked = string.IsNullOrWhiteSpace(address) ? "••••" : EmailMask.Mask(address);
        var pattern = options?.AllowedRecipientPattern?.Trim();
        if (string.IsNullOrEmpty(pattern))
        {
            return new Decision(false, masked, false);
        }

        if (IsListed(address, options?.AllowedRecipientAddresses))
        {
            return new Decision(false, masked, false);
        }

        var regex = Patterns.GetOrAdd(pattern, Compile);
        if (regex is null)
        {
            return new Decision(true, masked, true);
        }

        try
        {
            return new Decision(!regex.IsMatch(address), masked, false);
        }
        catch (RegexMatchTimeoutException)
        {
            return new Decision(true, masked, false);
        }
    }

    /// <summary>True the first time an invalid pattern is seen in this process.</summary>
    public static bool ConsumeInvalidPatternWarning()
        => Interlocked.Exchange(ref _invalidPatternWarned, 1) == 0;

    public static void ResetForTests()
    {
        Patterns.Clear();
        Interlocked.Exchange(ref _invalidPatternWarned, 0);
    }

    private static bool IsListed(string address, string[]? allowed)
    {
        if (string.IsNullOrWhiteSpace(address) || allowed is null)
        {
            return false;
        }

        foreach (var raw in allowed)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var candidate = MailAddressResolution.ExtractAddress(raw);
            if (string.Equals(candidate, address, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Regex? Compile(string pattern)
    {
        try
        {
            return new Regex(
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
                TimeSpan.FromMilliseconds(100));
        }
        catch (RegexParseException)
        {
            return null;
        }
    }
}
