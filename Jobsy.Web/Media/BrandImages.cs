namespace Jobsy.Web.Media;

/// <summary>Versioned, size-appropriate Lobsy mark URLs (WebP + PNG fallback).</summary>
public static class BrandImages
{
    public const string Version = "20260828-pin";
    public const string MascotVersion = "20260828-mascot";

    public const string Webp64 = $"images/brand/lobsy-64.webp?v={Version}";
    public const string Webp128 = $"images/brand/lobsy-128.webp?v={Version}";
    public const string Webp256 = $"images/brand/lobsy-256.webp?v={Version}";
    public const string Png128 = $"images/brand/lobsy-128.png?v={Version}";
    public const string AppleTouch = $"images/brand/lobsy-180.png?v={Version}";

    public const string AbsoluteWebp128 = $"/images/brand/lobsy-128.webp?v={Version}";
    public const string AbsoluteWebp256 = $"/images/brand/lobsy-256.webp?v={Version}";

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
}
