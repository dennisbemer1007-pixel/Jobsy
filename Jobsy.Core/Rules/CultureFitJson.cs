using System.Text.Json;

namespace Jobsy.Core.Rules;

public static class CultureFitJson
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static CultureFitResult? TryParse(string? json, CultureFitResult local)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(json, Options);
            if (dto is null)
            {
                return null;
            }

            var percent = Math.Clamp(dto.CultureFitPercent, 0, 100);
            var why = (dto.Why ?? "").Trim();
            if (why.Length < 12)
            {
                return null;
            }

            return CultureFitBuilder.ClampToLocal(
                local,
                new CultureFitResult(percent, CultureFitBuilder.Band(percent), CultureFitBuilder.Label(percent), why, true));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class Dto
    {
        public int CultureFitPercent { get; set; }
        public string? Why { get; set; }
    }
}
