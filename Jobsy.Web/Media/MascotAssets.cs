namespace Jobsy.Web.Media;

/// <summary>Mascot expression used by <see cref="Components.Brand.LobsyMascot"/>.</summary>
public enum MascotPose
{
    Rustig = 0,
    Uitleggen = 1,
    Nieuwsgierig = 2,
    GoedGedaan = 3,
    Nadenken = 4,
    Juichen = 5,
    Shell = 6,
}

/// <summary>How art is resolved for a pose.</summary>
public enum MascotArtFormat
{
    Fallback = 0,
    Svg = 1,
    Raster = 2,
    Inline = 3,
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
    bool IsInline,
    bool IsFallback,
    string? FallbackTransformClass,
    string Version);

/// <summary>
/// Manifest for public mascot art. Production uses hand-made inline SVG (see <see cref="Components.Brand.LobsyMascotArt"/>).
/// </summary>
public static class MascotAssets
{
    public const string Folder = "images/brand/mascot";
    public const string InlineVersion = "20261015-coral-mascot-traced-v2";

    private static readonly MascotArt[] DefaultArts =
    [
        new(MascotPose.Rustig, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.Uitleggen, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.Nieuwsgierig, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.GoedGedaan, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.Nadenken, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.Juichen, MascotArtFormat.Inline, InlineVersion),
        new(MascotPose.Shell, MascotArtFormat.Inline, InlineVersion),
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

    private static readonly int[] sourceArray = new[] { 64, 128, 256, 512 };

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

        if (art.Format == MascotArtFormat.Inline)
        {
            return new MascotSource(
                Src: "",
                SrcSet: null,
                AvifSrcSet: null,
                IsVector: true,
                IsInline: true,
                IsFallback: false,
                FallbackTransformClass: pose == MascotPose.Shell ? "pub-mascot--ghost" : $"pub-mascot--pose-{slug}",
                Version: art.Version);
        }

        if (art.Format == MascotArtFormat.Svg)
        {
            var src = $"{Folder}/mascot-{slug}.svg?v={art.Version}";
            return new MascotSource(
                Src: src,
                SrcSet: null,
                AvifSrcSet: null,
                IsVector: true,
                IsInline: false,
                IsFallback: false,
                FallbackTransformClass: null,
                Version: art.Version);
        }

        if (art.Format == MascotArtFormat.Raster)
        {
            var webp = string.Join(", ",
                sourceArray.Select(w => $"{Folder}/mascot-{slug}-{w}.webp?v={art.Version} {w}w"));
            var avif = string.Join(", ",
                sourceArray.Select(w => $"{Folder}/mascot-{slug}-{w}.avif?v={art.Version} {w}w"));
            var src = $"{Folder}/mascot-{slug}-256.webp?v={art.Version}";
            return new MascotSource(
                Src: src,
                SrcSet: webp,
                AvifSrcSet: avif,
                IsVector: false,
                IsInline: false,
                IsFallback: false,
                FallbackTransformClass: null,
                Version: art.Version);
        }

        var fbClass = pose == MascotPose.Shell
            ? "pub-mascot--ghost"
            : $"pub-mascot--fb-{slug}";

        return new MascotSource(
            Src: BrandImages.MascotWebp128,
            SrcSet: BrandImages.MascotSrcSet56,
            AvifSrcSet: null,
            IsVector: false,
            IsInline: false,
            IsFallback: true,
            FallbackTransformClass: fbClass,
            Version: BrandImages.MascotVersion);
    }

    public static string PoseSlug(MascotPose pose) => pose switch
    {
        MascotPose.Rustig => "rustig",
        MascotPose.Uitleggen => "uitleggen",
        MascotPose.Nieuwsgierig => "nieuwsgierig",
        MascotPose.GoedGedaan => "goed-gedaan",
        MascotPose.Nadenken => "nadenken",
        MascotPose.Juichen => "juichen",
        MascotPose.Shell => "shell",
        _ => "rustig",
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
