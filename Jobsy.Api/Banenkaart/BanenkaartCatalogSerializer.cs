using System.Security.Cryptography;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Api.Banenkaart;

/// <summary>Columnar banenkaart catalog for client-side Funda-style filtering (v1).</summary>
public static class BanenkaartCatalogSerializer
{
    public const int SchemaVersion = 1;

    public sealed record Result(byte[] Body, string ETag, int Count);

    public static Result Serialize(IReadOnlyList<VacancyDiscoveryRecord> records)
    {
        var visible = records
            .Where(r => double.IsFinite(r.Latitude) && double.IsFinite(r.Longitude))
            .ToList();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", SchemaVersion);
            writer.WriteNumber("n", visible.Count);

            WriteStringArray(writer, "id", visible, r => r.Id.ToString("D"));
            WriteNumberArray(writer, "lat", visible, r => Math.Round(r.Latitude, 5));
            WriteNumberArray(writer, "lng", visible, r => Math.Round(r.Longitude, 5));
            WriteStringArray(writer, "t", visible, r => r.Title);
            WriteStringArray(writer, "c", visible, r => r.CompanyName);
            WriteStringArray(writer, "ob", visible, r => r.OfferedByLabel ?? "");
            WriteIntArray(writer, "w", visible, r => (int)Math.Round(r.HourlyWage * 100m));
            WriteIntArray(writer, "h0", visible, r => (int)Math.Floor(r.MinHoursPerWeek ?? 0));
            WriteIntArray(writer, "h1", visible, r => (int)Math.Ceiling(r.MaxHoursPerWeek ?? 40));
            WriteStringArray(writer, "cat", visible, r => r.CategoryId?.ToString("D") ?? "");
            WriteStringArray(writer, "co", visible, r => r.CompanyId.ToString("D"));
            WriteIntArray(writer, "wt", visible, r => (int)r.WorkTypes);
            WriteIntArray(writer, "tr", visible, r => (int)r.RequiredTransport);
            WriteStringArray(writer, "col", visible, r =>
                r.SuitableFor65Plus
                    ? "#c9a227"
                    : r.CategoryColorHex ?? "");
            WriteIntArray(writer, "s65", visible, r => r.SuitableFor65Plus ? 1 : 0);
            WriteIntArray(writer, "pm", visible, r => r.PublicMapRadiusKm ?? 0);
            WriteStringArray(writer, "iu", visible, r => r.ImageUrl ?? "");

            writer.WriteEndObject();
        }

        var body = stream.ToArray();
        var etag = ComputeEtag(body);
        return new Result(body, etag, visible.Count);
    }

    private static string ComputeEtag(byte[] body)
    {
        var hash = SHA256.HashData(body);
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static void WriteStringArray<T>(
        Utf8JsonWriter writer,
        string name,
        IReadOnlyList<T> items,
        Func<T, string> select)
    {
        writer.WriteStartArray(name);
        foreach (var item in items)
        {
            writer.WriteStringValue(select(item));
        }

        writer.WriteEndArray();
    }

    private static void WriteNumberArray<T>(
        Utf8JsonWriter writer,
        string name,
        IReadOnlyList<T> items,
        Func<T, double> select)
    {
        writer.WriteStartArray(name);
        foreach (var item in items)
        {
            writer.WriteNumberValue(select(item));
        }

        writer.WriteEndArray();
    }

    private static void WriteIntArray<T>(
        Utf8JsonWriter writer,
        string name,
        IReadOnlyList<T> items,
        Func<T, int> select)
    {
        writer.WriteStartArray(name);
        foreach (var item in items)
        {
            writer.WriteNumberValue(select(item));
        }

        writer.WriteEndArray();
    }
}
