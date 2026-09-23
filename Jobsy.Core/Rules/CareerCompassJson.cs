using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Rules;

public static class CareerCompassJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(CareerCompassSnapshot snapshot)
        => JsonSerializer.Serialize(ToDto(snapshot), Options);

    public static CareerCompassSnapshot? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "null")
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<CompassDto>(json, Options);
            return dto is null ? null : CareerCompassSanitize.FromDto(dto, fromOpenAi: dto.FromOpenAi);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static CompassDto ToDto(CareerCompassSnapshot snapshot) => new()
    {
        Strengths = snapshot.Strengths.ToList(),
        SuperMatches = snapshot.SuperMatches.Select(ToItem).ToList(),
        StrongChoices = snapshot.StrongChoices.Select(ToItem).ToList(),
        Broadening = snapshot.Broadening.Select(ToItem).ToList(),
        PracticalNotes = snapshot.PracticalNotes.ToList(),
        FromDeepAnalysis = snapshot.FromDeepAnalysis,
        FromOpenAi = snapshot.FromOpenAi
    };

    private static OccupationDto ToItem(CareerOccupationMatch match) => new()
    {
        Title = match.Title,
        Percent = match.Percent,
        Band = match.Band,
        Why = match.Why,
        Keys = match.SearchKeys.ToList()
    };

    internal sealed class CompassDto
    {
        public List<string>? Strengths { get; set; }
        public List<OccupationDto>? SuperMatches { get; set; }
        public List<OccupationDto>? StrongChoices { get; set; }
        public List<OccupationDto>? Broadening { get; set; }
        public List<string>? PracticalNotes { get; set; }
        public bool FromDeepAnalysis { get; set; }
        public bool FromOpenAi { get; set; }
    }

    internal sealed class OccupationDto
    {
        public string? Title { get; set; }
        public int Percent { get; set; }
        public string? Band { get; set; }
        public string? Why { get; set; }
        public List<string>? Keys { get; set; }
    }
}
