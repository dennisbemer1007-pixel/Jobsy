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
                dto.FromOpenAi,
                dto.VacancyId is Guid vid
                    ? new RoleFitVacancyFit(
                        vid,
                        dto.BarrierKind ?? VacancyBarrierKind.Low.ToString(),
                        dto.CulturePercent,
                        dto.CultureBand,
                        dto.CultureLabel,
                        dto.CultureWhy,
                        (dto.FormalItems ?? [])
                            .Where(i => !string.IsNullOrWhiteSpace(i.Label))
                            .Select(i => new VacancyBarrierCheckItem(i.Key ?? "", i.Label!.Trim(), i.Met, i.Note ?? ""))
                            .ToList(),
                        dto.AvailabilityOk ?? true,
                        dto.ShowFormalBlock ?? false,
                        dto.ShowUpskill ?? false)
                    : null);
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
        FromOpenAi = snapshot.FromOpenAi,
        VacancyId = snapshot.VacancyFit?.VacancyId,
        BarrierKind = snapshot.VacancyFit?.BarrierKind,
        CulturePercent = snapshot.VacancyFit?.CulturePercent,
        CultureBand = snapshot.VacancyFit?.CultureBand,
        CultureLabel = snapshot.VacancyFit?.CultureLabel,
        CultureWhy = snapshot.VacancyFit?.CultureWhy,
        FormalItems = snapshot.VacancyFit?.FormalItems.Select(i => new FormalDto
        {
            Key = i.Key,
            Label = i.Label,
            Met = i.Met,
            Note = i.Note
        }).ToList(),
        AvailabilityOk = snapshot.VacancyFit?.AvailabilityOk,
        ShowFormalBlock = snapshot.VacancyFit?.ShowFormalBlock,
        ShowUpskill = snapshot.VacancyFit?.ShowUpskill
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
        public Guid? VacancyId { get; set; }
        public string? BarrierKind { get; set; }
        public int? CulturePercent { get; set; }
        public string? CultureBand { get; set; }
        public string? CultureLabel { get; set; }
        public string? CultureWhy { get; set; }
        public List<FormalDto>? FormalItems { get; set; }
        public bool? AvailabilityOk { get; set; }
        public bool? ShowFormalBlock { get; set; }
        public bool? ShowUpskill { get; set; }
    }

    private sealed class FormalDto
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
        public bool Met { get; set; }
        public string? Note { get; set; }
    }
}
