using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Models;

namespace Jobsy.Web.Bedrijvenkaart;

public static class InitiativeApplicationMapper
{
    public static ApplicationItem ToApplicationItem(WorkInitiativeListItemDto dto)
    {
        var status = MapStatus(dto.Status);
        var timeline = WorkInitiativeTimelineBuilder.Build(
            dto.CreatedAtUtc,
            dto.Status,
            dto.EmployerViewedAtUtc,
            dto.RespondedAtUtc);

        return new ApplicationItem
        {
            Id = dto.Id,
            VacancyId = Guid.Empty,
            VacancyTitle = string.IsNullOrWhiteSpace(dto.TeamName)
                ? $"Initiatief · {dto.CompanyName}"
                : $"{dto.TeamName} · {dto.CompanyName}",
            CompanyName = dto.CompanyName,
            CreatedAt = dto.CreatedAtUtc,
            Status = status,
            RespondedAt = timeline.ClosingAtUtc,
            IsInitiative = true,
            IsAutoDeclinedInitiative = dto.Status == WorkInitiativeStatus.AutoDeclined,
            TimelineClosingKey = timeline.ClosingLineKey,
            Timeline = timeline.Steps.Select(s => new ApplicationTimelineStepItem
            {
                Key = s.Key.ToString(),
                State = s.State.ToString(),
                OccurredAtUtc = s.OccurredAtUtc,
                LabelKey = s.LabelKey
            }).ToList(),
            LegacyNoHistory = timeline.LegacyNoHistory,
            NextStepKey = status is "Pending" or "Accepted" ? "Bk.Initiative.Next.Wait" : null
        };
    }

    private static string MapStatus(WorkInitiativeStatus status) => status switch
    {
        WorkInitiativeStatus.Pending => "Pending",
        WorkInitiativeStatus.Viewed => "Pending",
        WorkInitiativeStatus.Accepted => "Accepted",
        WorkInitiativeStatus.Declined => "Rejected",
        WorkInitiativeStatus.AutoDeclined => "Rejected",
        WorkInitiativeStatus.Withdrawn => "Withdrawn",
        _ => "Pending"
    };
}
