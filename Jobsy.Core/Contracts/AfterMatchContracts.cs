using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Contracts;

public sealed record AfterMatchThreadDto(
    Guid ApplicationId,
    Guid InterviewId,
    InterviewStatus InterviewStatus,
    AfterMatchContactShareState ContactShare,
    AfterMatchOutcomeKind? Outcome,
    string VacancyTitle,
    string CompanyName,
    string? TravelLabel,
    InterviewFormat Format,
    string? LocationOrLink,
    string? WithWhom,
    string? Note,
    DateTime? ScheduledStartUtc,
    DateTime? ScheduledEndUtc,
    IReadOnlyList<AfterMatchSlotDto> OpenSlots,
    IReadOnlyList<AfterMatchMessageDto> Messages,
    AfterMatchReminderPreferenceDto ReminderPreference,
    IReadOnlyList<AfterMatchChangeDto> RecentChanges,
    IReadOnlyList<AfterMatchSimilarVacancyDto> SimilarVacancies,
    string CandidateName,
    string? CandidateEmail,
    DateTime? AcceptedAtUtc,
    string? OutcomeReason);

public sealed record AfterMatchSlotDto(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    InterviewSlotProposer ProposedBy,
    string? Reason,
    bool IsSelected);

public sealed record AfterMatchMessageDto(
    Guid Id,
    bool IsMine,
    string SenderLabel,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    ApplicationMessageBlockReason BlockReason,
    bool SentDespiteWarning);

public sealed record AfterMatchReminderPreferenceDto(
    bool WeekBefore,
    bool DayBefore18,
    bool TwoHoursBefore);

public sealed record AfterMatchChangeDto(
    InterviewChangeKind Kind,
    string ActorRole,
    string Reason,
    DateTime CreatedAtUtc);

public sealed record AfterMatchSimilarVacancyDto(
    Guid VacancyId,
    string Title,
    string CompanyName,
    string TravelLabel);

public sealed record ProposeEmployerSlotsRequest(
    InterviewFormat Format,
    string? LocationOrLink,
    string? WithWhom,
    string? Note,
    IReadOnlyList<ProposedSlotRequest> Slots);

public sealed record ProposedSlotRequest(DateTime StartUtc, DateTime EndUtc);

public sealed record ProposeCandidateSlotRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason);

public sealed record SelectSlotRequest(Guid SlotId);

public sealed record RescheduleRequest(string Reason);

public sealed record SendMessageRequest(string Body, bool ConfirmSensitive);

public sealed record RecordOutcomeRequest(AfterMatchOutcomeKind Outcome, string Reason);

public sealed record UpdateReminderPreferenceRequest(
    bool WeekBefore,
    bool DayBefore18,
    bool TwoHoursBefore);

public sealed record SensitiveMessagePreview(
    SensitiveMessageContentRules.SensitiveKind Kind,
    bool WouldBlock,
    bool CanConfirmSend);
