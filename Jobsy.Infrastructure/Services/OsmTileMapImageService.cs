using System.Net.Http.Headers;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    public async Task<byte[]?> RenderAsync(
        double latitude,
        double longitude,
        int width = 640,
        int height = 280,
        int zoom = 15,
        byte[]? markerLogoPng = null,
        CancellationToken cancellationToken = default)
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

            using var canvas = new Image<Rgba32>(width, height, Color.ParseHex("dceef8"));
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

                    using var tile = Image.Load<Rgba32>(tileBytes);
                    var destX = (int)Math.Round(tx * TileSize - topLeftX);
                    var destY = (int)Math.Round(ty * TileSize - topLeftY);
                    canvas.Mutate(ctx => ctx.DrawImage(tile, new Point(destX, destY), 1f));
                    anyTile = true;
                }
            }

            if (!anyTile)
            {
                return null;
            }

            DrawMarker(canvas, width / 2, height / 2, markerLogoPng);

            await using var ms = new MemoryStream();
            await canvas.SaveAsPngAsync(ms, cancellationToken);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render candidate map image");
            return null;
        }
    }

    private static void DrawMarker(Image<Rgba32> canvas, int cx, int cy, byte[]? markerLogoPng)
    {
        var white = new Rgba32(255, 255, 255);
        var navy = new Rgba32(15, 45, 92);

        // Soft halo behind the brand mark (same spirit as map pins on the job map).
        FillCircle(canvas, cx, cy, 18, white);
        FillCircle(canvas, cx, cy, 16, navy);

        if (markerLogoPng is { Length: > 0 })
        {
            try
            {
                using var logo = Image.Load<Rgba32>(markerLogoPng);
                const int markSize = 26;
                logo.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(markSize, markSize),
                    Mode = ResizeMode.Max
                }));

                var destX = cx - logo.Width / 2;
                var destY = cy - logo.Height / 2;
                canvas.Mutate(ctx => ctx.DrawImage(logo, new Point(destX, destY), 1f));
                return;
            }
            catch
            {
                // Fall through to simple pin.
            }
        }

        FillCircle(canvas, cx, cy, 6, white);
    }

    private static void FillCircle(Image<Rgba32> canvas, int cx, int cy, int radius, Rgba32 color)
    {
        var r2 = radius * radius;
        for (var y = cy - radius; y <= cy + radius; y++)
        {
            for (var x = cx - radius; x <= cx + radius; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                if (dx * dx + dy * dy <= r2
                    && (uint)x < (uint)canvas.Width
                    && (uint)y < (uint)canvas.Height)
                {
                    canvas[x, y] = color;
                }
            }
        }
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
