using QRCoder;

namespace Jobsy.Infrastructure.Sales;

/// <summary>Shared QR PNG helper for sales flyers, materials and toolkit downloads (Dependencies F).</summary>
public static class SalesQr
{
    public static byte[] Png(string url, int pixelsPerModule = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelsPerModule, 1);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url.Trim(), QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }

    /// <summary>Render a QR whose module grid is roughly <paramref name="targetPixels"/> wide.</summary>
    public static byte[] PngForSize(string url, int targetPixels = 1024)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetPixels, 32);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url.Trim(), QRCodeGenerator.ECCLevel.Q);
        var modules = data.ModuleMatrix.Count;
        var pixelsPerModule = Math.Max(1, (int)Math.Round(targetPixels / (double)modules));
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
