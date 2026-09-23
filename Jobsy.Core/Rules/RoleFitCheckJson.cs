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
                            .Select(i => new VacancyBarrierCheckItem(i.Key ?? "", i.Label!.Trim(), i.Met, i.Note ?? "", i.Dealbreaker))
                            .ToList(),
                        dto.AvailabilityOk ?? true,
                        dto.ShowFormalBlock ?? false,
                        dto.ShowUpskill ?? false)
                    : null,
                (dto.SimilarRoles ?? [])
                    .Where(s => !string.IsNullOrWhiteSpace(s.Title))
                    .Select(s => new RoleFitSimilarRole(s.Title!.Trim(), s.Why ?? "", s.FitPercent))
                    .ToList(),
                ReadPath(dto.CareerPath));
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
            Note = i.Note,
            Dealbreaker = i.IsDealbreaker
        }).ToList(),
        AvailabilityOk = snapshot.VacancyFit?.AvailabilityOk,
        ShowFormalBlock = snapshot.VacancyFit?.ShowFormalBlock,
        ShowUpskill = snapshot.VacancyFit?.ShowUpskill,
        SimilarRoles = snapshot.SimilarRoles?.Select(s => new SimilarDto
        {
            Title = s.Title,
            Why = s.Why,
            FitPercent = s.FitPercent
        }).ToList(),
        CareerPath = snapshot.CareerPath is null ? null : new CareerPathDto
        {
            TotalMonths = snapshot.CareerPath.TotalMonths,
            DurationLabel = snapshot.CareerPath.DurationLabel,
            Summary = snapshot.CareerPath.Summary,
            Steps = snapshot.CareerPath.Steps.Select(s => new CareerPathStepDto
            {
                Order = s.Order,
                Title = s.Title,
                DurationMonths = s.DurationMonths,
                DurationLabel = s.DurationLabel,
                Detail = s.Detail
            }).ToList()
        }
    };

    private static CareerPathPlan? ReadPath(CareerPathDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Summary))
        {
            return null;
        }

        var steps = (dto.Steps ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Title))
            .Select(s => new CareerPathStep(
                s.Order,
                s.Title!.Trim(),
                Math.Max(0, s.DurationMonths),
                string.IsNullOrWhiteSpace(s.DurationLabel) ? CareerPathPlanner.FormatDuration(s.DurationMonths) : s.DurationLabel.Trim(),
                s.Detail?.Trim() ?? ""))
            .ToList();
        return new CareerPathPlan(
            Math.Max(0, dto.TotalMonths),
            string.IsNullOrWhiteSpace(dto.DurationLabel) ? CareerPathPlanner.FormatDuration(dto.TotalMonths) : dto.DurationLabel.Trim(),
            dto.Summary.Trim(),
            steps);
    }

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
        public List<SimilarDto>? SimilarRoles { get; set; }
        public CareerPathDto? CareerPath { get; set; }
    }

    private sealed class CareerPathDto
    {
        public int TotalMonths { get; set; }
        public string? DurationLabel { get; set; }
        public string? Summary { get; set; }
        public List<CareerPathStepDto>? Steps { get; set; }
    }

    private sealed class CareerPathStepDto
    {
        public int Order { get; set; }
        public string? Title { get; set; }
        public int DurationMonths { get; set; }
        public string? DurationLabel { get; set; }
        public string? Detail { get; set; }
    }

    private sealed class SimilarDto
    {
        public string? Title { get; set; }
        public string? Why { get; set; }
        public int FitPercent { get; set; }
    }

    private sealed class FormalDto
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
        public bool Met { get; set; }
        public string? Note { get; set; }
        public bool Dealbreaker { get; set; }
    }
}
