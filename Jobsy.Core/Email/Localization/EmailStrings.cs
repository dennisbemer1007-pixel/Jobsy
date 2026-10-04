using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Email.Localization;

/// <summary>
/// Mail string catalog (nl/en/pl/ro/ar). Values are plain text with {n} placeholders — never HTML.
/// </summary>
public static partial class EmailStrings
{
    private static readonly ConcurrentDictionary<string, int> FallbackHits = new(StringComparer.Ordinal);
    private static readonly Regex PlaceholderRx = new(@"\{(\d+)\}", RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["nl"] = Merge(EmailStringsNl.Map, EmailStringsComeback.For("nl")),
            ["en"] = Merge(EmailStringsEn.Map, EmailStringsComeback.For("en")),
            ["pl"] = Merge(EmailStringsPl.Map, EmailStringsComeback.For("pl")),
            ["ro"] = Merge(EmailStringsRo.Map, EmailStringsComeback.For("ro")),
            ["ar"] = Merge(EmailStringsAr.Map, EmailStringsComeback.For("ar"))
        };

    private static IReadOnlyDictionary<string, string> Merge(
        IReadOnlyDictionary<string, string> baseMap,
        IReadOnlyDictionary<string, string> extra)
    {
        var copy = new Dictionary<string, string>(baseMap, StringComparer.Ordinal);
        foreach (var pair in extra)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    public static IReadOnlyCollection<string> Languages { get; } = ["nl", "en", "pl", "ro", "ar"];

    /// <summary>Keys that fell back to nl since the last <see cref="ResetFallbackHits"/>.</summary>
    public static IReadOnlyDictionary<string, int> FallbackHitsSnapshot
        => new Dictionary<string, int>(FallbackHits, StringComparer.Ordinal);

    public static int FallbackHitCount => FallbackHits.Values.Sum();

    public static void ResetFallbackHits() => FallbackHits.Clear();

    public static string Get(EmailCulture culture, string key)
        => Get(culture.Language, key);

    public static string Get(string? language, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var lang = JobsyLanguages.Normalize(language);
        if (All.TryGetValue(lang, out var map) && map.TryGetValue(key, out var value))
        {
            return value;
        }

        if (!string.Equals(lang, "nl", StringComparison.OrdinalIgnoreCase)
            && All["nl"].TryGetValue(key, out var nl))
        {
            FallbackHits.AddOrUpdate(key, 1, static (_, n) => n + 1);
            return nl;
        }

        throw new KeyNotFoundException($"Missing EmailStrings key '{key}' (nl required).");
    }

    public static bool TryGet(string? language, string key, out string value)
    {
        value = string.Empty;
        try
        {
            value = Get(language, key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    public static EmailText Format(EmailCulture culture, string key, params EmailArg[] args)
        => EmailText.Format(Get(culture, key), args);

    public static string FormatRaw(EmailCulture culture, string key, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, Get(culture, key), args);

    public static string Reason(EmailCulture culture, string reasonKey, params object[] args)
    {
        var key = $"Email.Reason.{reasonKey}";
        var text = TryGet(culture.Language, key, out var raw)
            ? raw
            : Get(culture, "Email.Reason.Fallback");
        if (args.Length > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, text, args);
        }

        // Unformatted reasons that still contain {0} (company/inviter) fall back to a safe phrase.
        if (text.Contains("{0}", StringComparison.Ordinal))
        {
            return Get(culture, "Email.Reason.Fallback");
        }

        return text;
    }

    public static IReadOnlyCollection<string> KeysFor(string language)
        => All[JobsyLanguages.Normalize(language)].Keys.ToList();

    public static IReadOnlyCollection<string> AllKeys()
        => All["nl"].Keys.ToList();

    public static IReadOnlyList<int> PlaceholderIndexes(string value)
        => PlaceholderRx.Matches(value ?? string.Empty)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .Distinct()
            .OrderBy(i => i)
            .ToList();
}
