using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Rules;

public static class RoleFitCheckJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(RoleFitCheckSnapshot snapshot)
        => JsonSerializer.Serialize(ToDto(snapshot), Options);

    public static RoleFitCheckSnapshot? TryDeserialize(string? json, string jobTitle, bool fromDeepAnalysis)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "null")
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<FitDto>(json, Options);
            if (dto is null)
            {
                return null;
            }

            var title = RoleFitCheckBuilder.NormalizeTitle(dto.JobTitle) ?? jobTitle;
            var snapshot = new RoleFitCheckSnapshot(
                title,
                dto.MatchPercent,
                dto.Strengths ?? [],
                dto.Gaps ?? [],
                dto.ActionSteps ?? [],
                dto.SearchKeys ?? [],
                dto.FromDeepAnalysis || fromDeepAnalysis,
                dto.FromOpenAi);
            return RoleFitCheckBuilder.Sanitize(snapshot);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static FitDto ToDto(RoleFitCheckSnapshot snapshot) => new()
    {
        JobTitle = snapshot.JobTitle,
        MatchPercent = snapshot.MatchPercent,
        Strengths = snapshot.Strengths.ToList(),
        Gaps = snapshot.Gaps.ToList(),
        ActionSteps = snapshot.ActionSteps.ToList(),
        SearchKeys = snapshot.SearchKeys.ToList(),
        FromDeepAnalysis = snapshot.FromDeepAnalysis,
        FromOpenAi = snapshot.FromOpenAi
    };

    private sealed class FitDto
    {
        public string? JobTitle { get; set; }
        public int MatchPercent { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? Gaps { get; set; }
        public List<string>? ActionSteps { get; set; }
        public List<string>? SearchKeys { get; set; }
        public bool FromDeepAnalysis { get; set; }
        public bool FromOpenAi { get; set; }
    }
}
