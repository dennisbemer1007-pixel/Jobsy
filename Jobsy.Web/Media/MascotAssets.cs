namespace Jobsy.Web.Media;

/// <summary>Mascot pose slug used by LobsyMascot and the asset contract.</summary>
public enum MascotPose
{
    Default = 0,
    Waving = 1,
    Diving = 2,
    Sitting = 3,
    Celebrating = 4,
    Shell = 5,
}

/// <summary>How art is resolved for a pose. Every pose starts as <see cref="Fallback"/>.</summary>
public enum MascotArtFormat
{
    Fallback = 0,
    Svg = 1,
    Raster = 2,
}

/// <summary>CSS box / intrinsic size token for <c>LobsyMascot</c>.</summary>
public enum MascotSize
{
    Tiny = 40,
    Small = 64,
    Medium = 128,
    Large = 200,
    Hero = 256,
}

/// <summary>Per-pose art entry in the manifest.</summary>
public sealed record MascotArt(MascotPose Pose, MascotArtFormat Format, string Version);

/// <summary>Resolved image URLs and flags for one pose.</summary>
public sealed record MascotSource(
    string Src,
    string? SrcSet,
    string? AvifSrcSet,
    bool IsVector,
    bool IsFallback,
    string? FallbackTransformClass,
    string Version);

/// <summary>
/// Manifest for public mascot art. Flip <see cref="MascotArt.Format"/> and bump
/// <see cref="MascotArt.Version"/> when designer files land — no markup changes.
/// See <c>docs/brand/mascot-assets.md</c>.
/// </summary>
public static class MascotAssets
{
    public const string Folder = "images/brand/mascot";

    private static readonly MascotArt[] DefaultArts =
    [
        new(MascotPose.Default, MascotArtFormat.Fallback, BrandImages.MascotVersion),
        new(MascotPose.Waving, MascotArtFormat.Fallback, BrandImages.MascotVersion),
        new(MascotPose.Diving, MascotArtFormat.Fallback, BrandImages.MascotVersion),
        new(MascotPose.Sitting, MascotArtFormat.Fallback, BrandImages.MascotVersion),
        new(MascotPose.Celebrating, MascotArtFormat.Fallback, BrandImages.MascotVersion),
        new(MascotPose.Shell, MascotArtFormat.Fallback, BrandImages.MascotVersion),
    ];

    private static readonly AsyncLocal<IReadOnlyDictionary<MascotPose, MascotArt>?> Override = new();

    /// <summary>Production (or overridden) art entries, one per pose.</summary>
    public static IReadOnlyList<MascotArt> Arts
    {
        get
        {
            if (Override.Value is null)
            {
                return DefaultArts;
            }

            return Enum.GetValues<MascotPose>().Select(GetArt).ToArray();
        }
    }

    public static MascotArt GetArt(MascotPose pose)
    {
        if (Override.Value is not null && Override.Value.TryGetValue(pose, out var over))
        {
            return over;
        }

        return DefaultArts[(int)pose];
    }

    /// <summary>Resolve URLs and fallback CSS for a pose.</summary>
    public static MascotSource Resolve(MascotPose pose)
    {
        var art = GetArt(pose);
        var slug = PoseSlug(pose);

        if (art.Format == MascotArtFormat.Svg)
        {
            var src = $"{Folder}/mascot-{slug}.svg?v={art.Version}";
            return new MascotSource(
                Src: src,
                SrcSet: null,
                AvifSrcSet: null,
                IsVector: true,
                IsFallback: false,
                FallbackTransformClass: null,
                Version: art.Version);
        }

        if (art.Format == MascotArtFormat.Raster)
        {
            var webp = string.Join(", ",
                new[] { 64, 128, 256, 512 }.Select(w => $"{Folder}/mascot-{slug}-{w}.webp?v={art.Version} {w}w"));
            var avif = string.Join(", ",
                new[] { 64, 128, 256, 512 }.Select(w => $"{Folder}/mascot-{slug}-{w}.avif?v={art.Version} {w}w"));
            var src = $"{Folder}/mascot-{slug}-256.webp?v={art.Version}";
            return new MascotSource(
                Src: src,
                SrcSet: webp,
                AvifSrcSet: avif,
                IsVector: false,
                IsFallback: false,
                FallbackTransformClass: null,
                Version: art.Version);
        }

        // Fallback: today's single-pose mascot (+ CSS transform / ghost).
        var fbClass = pose == MascotPose.Shell
            ? "pub-mascot--ghost"
            : $"pub-mascot--fb-{slug}";

        return new MascotSource(
            Src: BrandImages.MascotWebp128,
            SrcSet: BrandImages.MascotSrcSet56,
            AvifSrcSet: null,
            IsVector: false,
            IsFallback: true,
            FallbackTransformClass: fbClass,
            Version: BrandImages.MascotVersion);
    }

    public static string PoseSlug(MascotPose pose) => pose switch
    {
        MascotPose.Default => "default",
        MascotPose.Waving => "waving",
        MascotPose.Diving => "diving",
        MascotPose.Sitting => "sitting",
        MascotPose.Celebrating => "celebrating",
        MascotPose.Shell => "shell",
        _ => "default",
    };

    public static string SizeCssClass(MascotSize size) => size switch
    {
        MascotSize.Tiny => "pub-mascot--tiny",
        MascotSize.Small => "pub-mascot--small",
        MascotSize.Medium => "pub-mascot--medium",
        MascotSize.Large => "pub-mascot--large",
        MascotSize.Hero => "pub-mascot--hero",
        _ => "pub-mascot--medium",
    };

    public static string SizesAttribute(MascotSize size) => $"{(int)size}px";

    /// <summary>
    /// Temporary override for tests (Svg/Raster markup without committing art).
    /// Dispose to restore production defaults.
    /// </summary>
    public static IDisposable OverrideArts(params MascotArt[] arts)
        => new ArtOverride(arts);

    private sealed class ArtOverride : IDisposable
    {
        private readonly IReadOnlyDictionary<MascotPose, MascotArt>? _previous;

        public ArtOverride(MascotArt[] arts)
        {
            _previous = Override.Value;
            var map = new Dictionary<MascotPose, MascotArt>();
            foreach (var a in DefaultArts)
            {
                map[a.Pose] = a;
            }

            foreach (var a in arts)
            {
                map[a.Pose] = a;
            }

            Override.Value = map;
        }

        public void Dispose() => Override.Value = _previous;
    }
}
