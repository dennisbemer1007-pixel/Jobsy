using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

public static class CandidateMatchSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(IReadOnlyList<CandidateMatchedVacancyDto> matches)
        => JsonSerializer.Serialize(matches, Options);

    public static IReadOnlyList<CandidateMatchedVacancyDto> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CandidateMatchedVacancyDto>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
