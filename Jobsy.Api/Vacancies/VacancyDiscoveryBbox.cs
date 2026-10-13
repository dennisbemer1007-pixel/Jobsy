using Jobsy.Core.Contracts;

namespace Jobsy.Api.Vacancies;

public readonly record struct GeoBbox(double MinLat, double MinLng, double MaxLat, double MaxLng)
{
    public bool IsValid =>
        MinLat is >= -90 and <= 90
        && MaxLat is >= -90 and <= 90
        && MinLng is >= -180 and <= 180
        && MaxLng is >= -180 and <= 180
        && MinLat <= MaxLat
        && Math.Abs(MinLat - MaxLat) > 1e-9
        && Math.Abs(MinLng - MaxLng) > 1e-9;

    public bool Contains(double lat, double lng)
    {
        if (!IsValid)
        {
            return true;
        }

        return lat >= MinLat && lat <= MaxLat && lng >= MinLng && lng <= MaxLng;
    }

    public static bool TryParse(
        double? minLat,
        double? minLng,
        double? maxLat,
        double? maxLng,
        out GeoBbox bbox)
    {
        bbox = default;
        if (minLat is null || minLng is null || maxLat is null || maxLng is null)
        {
            return false;
        }

        bbox = new GeoBbox(minLat.Value, minLng.Value, maxLat.Value, maxLng.Value);
        return bbox.IsValid;
    }
}

public static class VacancyDiscoveryBbox
{
    public static IEnumerable<VacancyDiscoveryRecord> Filter(
        IEnumerable<VacancyDiscoveryRecord> records,
        GeoBbox? bbox)
    {
        if (bbox is not { IsValid: true } box)
        {
            return records;
        }

        return records.Where(r => box.Contains(r.Latitude, r.Longitude));
    }

    /// <summary>
    /// Spatial grid sample so low-zoom bbox requests stay bounded while covering the viewport.
    /// Prefers highlighted vacancies within each cell.
    /// </summary>
    public static List<VacancyDiscoveryRecord> CapPins(
        IReadOnlyList<VacancyDiscoveryRecord> records,
        GeoBbox? bbox,
        int maxCount,
        int? zoom)
    {
        if (records.Count <= maxCount)
        {
            return records.ToList();
        }

        var box = bbox is { IsValid: true } b
            ? b
            : InferBbox(records);
        if (!box.IsValid)
        {
            return records.Take(maxCount).ToList();
        }

        var z = zoom is int zv ? Math.Clamp(zv, 1, 22) : 10;
        var targetCells = Math.Min(maxCount, GridCellTarget(z));
        var latSpan = Math.Max(box.MaxLat - box.MinLat, 1e-6);
        var lngSpan = Math.Max(box.MaxLng - box.MinLng, 1e-6);
        var rows = Math.Max(1, (int)Math.Round(Math.Sqrt(targetCells * (latSpan / lngSpan))));
        var cols = Math.Max(1, (int)Math.Ceiling((double)targetCells / rows));
        var cellLat = latSpan / rows;
        var cellLng = lngSpan / cols;

        var cells = new Dictionary<(int R, int C), VacancyDiscoveryRecord>();
        foreach (var r in records)
        {
            var row = (int)Math.Floor((r.Latitude - box.MinLat) / cellLat);
            var col = (int)Math.Floor((r.Longitude - box.MinLng) / cellLng);
            row = Math.Clamp(row, 0, rows - 1);
            col = Math.Clamp(col, 0, cols - 1);
            var key = (row, col);
            if (!cells.TryGetValue(key, out var existing))
            {
                cells[key] = r;
                continue;
            }

            if (PreferPin(r, existing))
            {
                cells[key] = r;
            }
        }

        var sampled = cells.Values.ToList();
        if (sampled.Count > maxCount)
        {
            sampled = sampled.Take(maxCount).ToList();
        }

        return sampled;
    }

    private static int GridCellTarget(int zoom)
        => zoom switch
        {
            <= 7 => 120,
            <= 9 => 280,
            <= 11 => 600,
            <= 13 => 1200,
            _ => 2500
        };

    private static bool PreferPin(VacancyDiscoveryRecord candidate, VacancyDiscoveryRecord incumbent)
    {
        if (candidate.IsHighlighted != incumbent.IsHighlighted)
        {
            return candidate.IsHighlighted;
        }

        return string.Compare(candidate.Title, incumbent.Title, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static GeoBbox InferBbox(IReadOnlyList<VacancyDiscoveryRecord> records)
    {
        if (records.Count == 0)
        {
            return default;
        }

        var minLat = records.Min(r => r.Latitude);
        var maxLat = records.Max(r => r.Latitude);
        var minLng = records.Min(r => r.Longitude);
        var maxLng = records.Max(r => r.Longitude);
        return new GeoBbox(minLat, minLng, maxLat, maxLng);
    }
}
