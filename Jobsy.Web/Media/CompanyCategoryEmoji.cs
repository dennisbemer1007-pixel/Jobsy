namespace Jobsy.Web.Media;

/// <summary>
/// Emoji tile for a company page without an uploaded logo (public-pages 09, D4). Keyword based on
/// the public vacancy category name, so no extra public data is needed.
/// </summary>
public static class CompanyCategoryEmoji
{
    public const string Fallback = "🏢";

    private static readonly (string Keyword, string Glyph)[] Map =
    [
        ("bakker", "🥐"),
        ("horeca", "🍽️"),
        ("restaurant", "🍽️"),
        ("cafe", "☕"),
        ("café", "☕"),
        ("winkel", "🛍️"),
        ("retail", "🛍️"),
        ("supermarkt", "🛒"),
        ("zorg", "🩺"),
        ("kinder", "🧸"),
        ("onderwijs", "🎓"),
        ("school", "🎓"),
        ("bouw", "🧱"),
        ("techn", "🔧"),
        ("installat", "🔧"),
        ("transport", "🚚"),
        ("logistiek", "📦"),
        ("tuin", "🌷"),
        ("kas", "🌷"),
        ("agrar", "🌾"),
        ("schoonmaak", "🧹"),
        ("kantoor", "🗂️"),
        ("administrat", "🗂️"),
        ("ict", "💻"),
        ("software", "💻"),
        ("sport", "🏅"),
        ("beveilig", "🛡️"),
        ("haar", "💇"),
        ("beauty", "💇")
    ];

    public static string For(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return Fallback;
        }

        var name = categoryName.Trim().ToLowerInvariant();
        foreach (var (keyword, glyph) in Map)
        {
            if (name.Contains(keyword, StringComparison.Ordinal))
            {
                return glyph;
            }
        }

        return Fallback;
    }
}
