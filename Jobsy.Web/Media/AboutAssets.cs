namespace Jobsy.Web.Media;

/// <summary>
/// Manifest for the <c>/wie-zijn-wij</c> founder photo (public-pages 08). Flip
/// <see cref="HasFounderPhoto"/> to <c>true</c> when <c>wwwroot/images/about/founder.webp</c>
/// lands; until then the card shows the emoji avatar. <c>AboutPageTests</c> keeps the flag and
/// the file in sync.
/// </summary>
public static class AboutAssets
{
    public const string FounderPhotoPath = "images/about/founder.webp";

    public const bool HasFounderPhoto = false;

    public const string Version = "20261001-about";

    public static string FounderPhotoUrl => $"/{FounderPhotoPath}?v={Version}";
}
