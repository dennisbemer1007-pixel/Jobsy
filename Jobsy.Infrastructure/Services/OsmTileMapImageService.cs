using System.Net.Http.Headers;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Media;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Composites OpenStreetMap raster tiles into a static map PNG with a Lobsy logo pin.
/// Fails soft (null) when tiles cannot be fetched — PDF still renders without the map.
/// </summary>
public sealed class OsmTileMapImageService : ICandidateMapImageService
{
    private const int TileSize = 256;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OsmTileMapImageService> _logger;

    public OsmTileMapImageService(
        IHttpClientFactory httpClientFactory,
        ILogger<OsmTileMapImageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<byte[]?> RenderAsync(
        double latitude,
        double longitude,
        int width = 640,
        int height = 280,
        int zoom = 15,
        byte[]? markerLogoPng = null,
        CancellationToken cancellationToken = default)
        => RenderInternalAsync(
            latitude,
            longitude,
            width,
            height,
            zoom,
            radiusMeters: null,
            markerLogoPng,
            cancellationToken);

    public Task<byte[]?> RenderWorkplaceReachAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        int width = 640,
        int height = 280,
        byte[]? markerLogoPng = null,
        CancellationToken cancellationToken = default)
    {
        radiusMeters = Math.Clamp(radiusMeters, 40, 80_000);
        var zoom = ZoomToFitRadius(latitude, radiusMeters, width, height);
        return RenderInternalAsync(
            latitude,
            longitude,
            width,
            height,
            zoom,
            radiusMeters,
            markerLogoPng,
            cancellationToken);
    }

    private async Task<byte[]?> RenderInternalAsync(
        double latitude,
        double longitude,
        int width,
        int height,
        int zoom,
        double? radiusMeters,
        byte[]? markerLogoPng,
        CancellationToken cancellationToken)
    {
        if (latitude is < -85 or > 85 || longitude is < -180 or > 180)
        {
            return null;
        }

        width = Math.Clamp(width, 120, 1200);
        height = Math.Clamp(height, 80, 800);
        zoom = Math.Clamp(zoom, 10, 17);

        try
        {
            var (centerX, centerY) = LatLonToPixel(latitude, longitude, zoom);
            var topLeftX = centerX - width / 2.0;
            var topLeftY = centerY - height / 2.0;
            var startTileX = (int)Math.Floor(topLeftX / TileSize);
            var startTileY = (int)Math.Floor(topLeftY / TileSize);
            var endTileX = (int)Math.Floor((topLeftX + width) / TileSize);
            var endTileY = (int)Math.Floor((topLeftY + height) / TileSize);
            var n = 1 << zoom;

            using var bitmap = SkiaImageCodec.CreateSolid(width, height, SKColor.Parse("#dceef8"));
            using var canvas = new SKCanvas(bitmap);
            var client = _httpClientFactory.CreateClient("OsmTiles");
            var anyTile = false;

            for (var ty = startTileY; ty <= endTileY; ty++)
            {
                if (ty < 0 || ty >= n)
                {
                    continue;
                }

                for (var tx = startTileX; tx <= endTileX; tx++)
                {
                    var wrappedX = ((tx % n) + n) % n;
                    var url = $"https://tile.openstreetmap.org/{zoom}/{wrappedX}/{ty}.png";
                    byte[]? tileBytes;
                    try
                    {
                        using var response = await client.GetAsync(url, cancellationToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        tileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        _logger.LogDebug(ex, "OSM tile fetch failed for {Url}", url);
                        continue;
                    }

                    using var tile = SkiaImageCodec.Decode(tileBytes);
                    var destX = (int)Math.Round(tx * TileSize - topLeftX);
                    var destY = (int)Math.Round(ty * TileSize - topLeftY);
                    canvas.DrawBitmap(tile, destX, destY);
                    anyTile = true;
                }
            }

            if (!anyTile)
            {
                return null;
            }

            if (radiusMeters is double meters and > 0)
            {
                var radiusPx = (int)Math.Round(MetersToPixels(latitude, meters, zoom));
                radiusPx = Math.Clamp(radiusPx, 8, Math.Min(width, height) / 2 - 4);
                DrawReachCircle(canvas, width / 2, height / 2, radiusPx);
            }

            DrawMarker(canvas, width / 2, height / 2, markerLogoPng);
            canvas.Flush();
            return SkiaImageCodec.EncodePng(bitmap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render candidate map image");
            return null;
        }
    }

    /// <summary>
    /// Chooses the highest zoom where the circle still fits inside the image with padding.
    /// </summary>
    internal static int ZoomToFitRadius(double latitude, double radiusMeters, int width, int height)
    {
        var maxPx = Math.Min(width, height) * 0.42;
        for (var zoom = 17; zoom >= 10; zoom--)
        {
            var px = MetersToPixels(latitude, radiusMeters, zoom);
            if (px <= maxPx)
            {
                return zoom;
            }
        }

        return 10;
    }

    private static double MetersToPixels(double latitude, double meters, int zoom)
    {
        var metersPerPixel = 156543.03392 * Math.Cos(latitude * Math.PI / 180.0) / Math.Pow(2, zoom);
        if (metersPerPixel <= 0)
        {
            return 0;
        }

        return meters / metersPerPixel;
    }

    private static void DrawReachCircle(SKCanvas canvas, int cx, int cy, int radius)
    {
        using var fill = new SKPaint
        {
            Color = new SKColor(51, 136, 255, 48),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(cx, cy, radius, fill);
        using var ring = new SKPaint
        {
            Color = new SKColor(51, 136, 255, 220),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawCircle(cx, cy, radius, ring);
    }

    private static void DrawMarker(SKCanvas canvas, int cx, int cy, byte[]? markerLogoPng)
    {
        using var whiteFill = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
        using var navyFill = new SKPaint { Color = new SKColor(15, 45, 92), IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(cx, cy, 18, whiteFill);
        canvas.DrawCircle(cx, cy, 16, navyFill);

        if (markerLogoPng is { Length: > 0 })
        {
            try
            {
                using var logo = SkiaImageCodec.Decode(markerLogoPng);
                const int markSize = 26;
                using var scaled = SkiaImageCodec.ResizeToMaxExtent(logo, markSize);
                var destX = cx - scaled.Width / 2f;
                var destY = cy - scaled.Height / 2f;
                canvas.DrawBitmap(scaled, destX, destY);
                return;
            }
            catch
            {
                // Fall through to simple pin.
            }
        }

        canvas.DrawCircle(cx, cy, 6, whiteFill);
    }

    private static (double X, double Y) LatLonToPixel(double lat, double lon, int zoom)
    {
        var n = 1 << zoom;
        var x = (lon + 180.0) / 360.0 * n * TileSize;
        var latRad = lat * Math.PI / 180.0;
        var y = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n * TileSize;
        return (x, y);
    }

    public static void ConfigureHttpClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(8);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LobsyCV/1.0 (candidate-map; contact=hello@lobsy.nl)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
    }
}
