using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Infrastructure.Media;

/// <summary>
/// Small WebP thumbnails for banenkaart map popups (load on open, not with pins/list).
/// </summary>
public static class VacancyMapPopupPhotoEncoder
{
    public const int DesktopWidth = 640;
    public const int MobileWidth = 248;
    public const int Quality = 70;

    public static int WidthForVariant(string? variant)
        => string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
            ? MobileWidth
            : DesktopWidth;

    public static byte[] ToWebp(ReadOnlySpan<byte> sourceBytes, int targetWidth)
    {
        using var image = Image.Load(sourceBytes);
        if (image.Width > targetWidth)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, 0),
                Mode = ResizeMode.Max
            }));
        }

        using var ms = new MemoryStream();
        image.SaveAsWebp(ms, new WebpEncoder { Quality = Quality });
        return ms.ToArray();
    }
}
