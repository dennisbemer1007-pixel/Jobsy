using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>Auth keys (login / MFA / password reset) must have real pl/ro/ar, matching placeholders.</summary>
public sealed class AuthLocalizationGuardTests
{
    private static readonly string[] AuthPrefixes =
    [
        "Login.",
        "Mfa.",
        "ForgotPassword.",
        "SetPassword.",
        "Auth."
    ];

    private static readonly HashSet<string> AllowIdenticalToEn = new(StringComparer.Ordinal)
    {
        "Lobsy",
        "Microsoft",
        "Google",
        "Print",
        ".txt"
    };

    [Fact]
    public void Auth_pl_ro_ar_differ_from_en_placeholders_match_and_non_empty()
    {
        var catalog = LoadCatalog();
        Assert.True(catalog.ContainsKey("nl"));
        var nl = catalog["nl"];
        var en = catalog["en"];
        var pl = catalog["pl"];
        var ro = catalog["ro"];
        var ar = catalog["ar"];

        var authKeys = nl.Keys
            .Where(k => AuthPrefixes.Any(p => k.StartsWith(p, StringComparison.Ordinal)))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(authKeys.Count >= 80, $"Expected many auth keys, got {authKeys.Count}");

        var identical = new List<string>();
        var placeholderMismatches = new List<string>();
        var empty = new List<string>();

        foreach (var key in authKeys)
        {
            foreach (var (lang, map) in new[] { ("en", en), ("pl", pl), ("ro", ro), ("ar", ar) })
            {
                Assert.True(map.ContainsKey(key), $"Missing {lang}:{key}");
                if (string.IsNullOrWhiteSpace(map[key]))
                {
                    empty.Add($"{lang}:{key}");
                }
            }

            var enVal = en[key];
            foreach (var (lang, map) in new[] { ("pl", pl), ("ro", ro), ("ar", ar) })
            {
                var val = map[key];
                if (string.Equals(val, enVal, StringComparison.Ordinal)
                    && !IsAllowedIdentical(enVal))
                {
                    identical.Add($"{lang}:{key}");
                }

                if (!PlaceholdersMatch(nl[key], val))
                {
                    placeholderMismatches.Add($"{lang}:{key}");
                }
            }
        }

        Assert.True(
            empty.Count == 0,
            "Empty auth values:\n" + string.Join("\n", empty.Take(20)));
        Assert.True(
            identical.Count == 0,
            "Auth pl/ro/ar still equal to en:\n" + string.Join("\n", identical.Take(40)));
        Assert.True(
            placeholderMismatches.Count == 0,
            "Placeholder mismatch vs nl:\n" + string.Join("\n", placeholderMismatches.Take(40)));
    }

    private static bool IsAllowedIdentical(string en)
    {
        var trimmed = en.Trim();
        if (AllowIdenticalToEn.Contains(trimmed))
        {
            return true;
        }

        if (trimmed.Contains("Microsoft", StringComparison.Ordinal)
            || trimmed.Contains("Google", StringComparison.Ordinal)
            || trimmed.Contains("Lobsy", StringComparison.Ordinal))
        {
            if (trimmed.Length <= 24 && Regex.IsMatch(trimmed, @"^[A-Za-z0-9 .·\-]+$"))
            {
                return true;
            }
        }

        if (Regex.IsMatch(trimmed, @"^[\d\{\}\s/\-A-Z0-9]+$") && trimmed.Length <= 24)
        {
            return true;
        }

        return false;
    }

    private static bool PlaceholdersMatch(string a, string b)
    {
        static IEnumerable<string> Tokens(string s)
            => Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x);

        return Tokens(a).SequenceEqual(Tokens(b));
    }

    private static Dictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        var catalogField = typeof(UiStrings).GetField(
            "Catalog",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(catalogField);
        return (Dictionary<string, Dictionary<string, string>>)catalogField!.GetValue(null)!;
    }
}
