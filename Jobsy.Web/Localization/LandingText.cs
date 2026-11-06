using Jobsy.Web.Features;

namespace Jobsy.Web.Localization;

/// <summary>
/// Picks <c>key.Zw</c> when the variant is OFF and that sibling exists; otherwise <paramref name="key"/>.
/// </summary>
public static class LandingText
{
    public static string For(string key, LandingVariant variant, string? language = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (variant == LandingVariant.Zw)
        {
            var zwKey = key + ".Zw";
            var zwValue = UiStrings.Get(zwKey, language);
            if (!string.Equals(zwValue, zwKey, StringComparison.Ordinal))
            {
                return zwValue;
            }
        }

        return UiStrings.Get(key, language);
    }
}
