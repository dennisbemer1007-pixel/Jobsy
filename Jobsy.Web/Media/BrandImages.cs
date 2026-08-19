namespace Jobsy.Web.Media;

/// <summary>Versioned, size-appropriate Lobsy mark URLs (WebP + PNG fallback).</summary>
public static class BrandImages
{
    public const string Version = "20260816-perf";

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
}
