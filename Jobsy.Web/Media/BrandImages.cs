namespace Jobsy.Web.Media;

/// <summary>Versioned Lobsy brand asset URLs (variant E, wwwroot/brand).</summary>
public static class BrandImages
{
    public const string Version = "20261018-23";
    public const string MascotVersion = "20261004-13";

    public const string BrandRoot = "brand";

    public const string AppIconSvg = $"/{BrandRoot}/svg/lobsy-app-icoon.svg?v={Version}";
    public const string MarkSvg = $"/{BrandRoot}/svg/lobsy-merkteken.svg?v={Version}";
    public const string MarkDarkSvg = $"/{BrandRoot}/svg/lobsy-merkteken-donker.svg?v={Version}";
    /// <summary>Cream lobster silhouette on employer/admin chat launcher (direction C).</summary>
    public const string ChatSilhouetteSvg = $"/{BrandRoot}/svg/lobsy-monochroom-wit.svg?v={Version}";
    public const string HorizontalSvg = $"/{BrandRoot}/svg/lobsy-logo-horizontaal.svg?v={Version}";
    public const string HorizontalDarkSvg = $"/{BrandRoot}/svg/lobsy-logo-horizontaal-donker.svg?v={Version}";
    public const string StackedSvg = $"/{BrandRoot}/svg/lobsy-logo-gestapeld.svg?v={Version}";
    public const string StackedDarkSvg = $"/{BrandRoot}/svg/lobsy-logo-gestapeld-donker.svg?v={Version}";

    public const string OgImage = $"/{BrandRoot}/png/lobsy-og-image.png?v={Version}";

    public const string Webp64 = $"images/brand/lobsy-64.webp?v={Version}";
    public const string Webp128 = $"images/brand/lobsy-128.webp?v={Version}";
    public const string Webp256 = $"images/brand/lobsy-256.webp?v={Version}";
    public const string Png128 = $"images/brand/lobsy-128.png?v={Version}";
    public const string AppleTouch = $"/{BrandRoot}/png/lobsy-apple-touch-icon-180.png?v={Version}";

    public const string AbsoluteWebp128 = $"/images/brand/lobsy-128.webp?v={Version}";
    public const string AbsoluteWebp256 = $"/images/brand/lobsy-256.webp?v={Version}";
    public const string AbsoluteOgImage = OgImage;

    public const string SrcSet56 =
        $"{Webp64} 64w, {Webp128} 128w, {Webp256} 256w";

    public const string SrcSet120 =
        $"{Webp128} 128w, {Webp256} 256w";

    /// <summary>Illustrated mascot kept for chatbots / sollicitatiebots.</summary>
    public const string MascotWebp64 = $"images/brand/mascot-64.webp?v={MascotVersion}";
    public const string MascotWebp128 = $"images/brand/mascot-128.webp?v={MascotVersion}";
    public const string MascotWebp256 = $"images/brand/mascot-256.webp?v={MascotVersion}";
    public const string MascotPng128 = $"images/brand/mascot-128.png?v={MascotVersion}";

    public const string MascotSrcSet56 =
        $"{MascotWebp64} 64w, {MascotWebp128} 128w, {MascotWebp256} 256w";

    public static string ForVariant(LobsyLogoVariant variant) => variant switch
    {
        LobsyLogoVariant.Horizontal => HorizontalSvg,
        LobsyLogoVariant.HorizontalDark => HorizontalDarkSvg,
        LobsyLogoVariant.Stacked => StackedSvg,
        _ => MarkSvg,
    };

    public static string CssModifier(LobsyLogoVariant variant) => variant switch
    {
        LobsyLogoVariant.Horizontal => "lobsy-logo--horizontal",
        LobsyLogoVariant.HorizontalDark => "lobsy-logo--horizontal lobsy-logo--horizontal-dark",
        LobsyLogoVariant.Stacked => "lobsy-logo--stacked",
        _ => "lobsy-logo--mark",
    };
}
