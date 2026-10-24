using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IAfterMatchService
{
    Task<AfterMatchThreadDto?> GetThreadForCandidateAsync(
        Guid applicationId,
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto?> GetThreadForEmployerAsync(
        Guid applicationId,
        Guid employerUserId,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> EnsureThreadAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> ProposeEmployerSlotsAsync(
        Guid applicationId,
        Guid employerUserId,
        ProposeEmployerSlotsRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> ProposeCandidateSlotAsync(
        Guid applicationId,
        Guid candidateUserId,
        ProposeCandidateSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> SelectSlotAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        SelectSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> RescheduleAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        RescheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> CancelInterviewAsync(
        Guid applicationId,
        Guid userId,
        bool isEmployer,
        RescheduleRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> SendMessageAsync(
        Guid applicationId,
        Guid senderUserId,
        bool senderIsEmployer,
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task MarkCandidateMessagesReadAsync(
        Guid applicationId,
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<SensitiveMessagePreview> PreviewMessageAsync(
        Guid applicationId,
        Guid senderUserId,
        bool senderIsEmployer,
        string body,
        CancellationToken cancellationToken = default);

    Task<AfterMatchThreadDto> RecordOutcomeAsync(
        Guid applicationId,
        Guid employerUserId,
        RecordOutcomeRequest request,
        CancellationToken cancellationToken = default);

    Task<AfterMatchReminderPreferenceDto> UpdateReminderPreferenceAsync(
        Guid candidateUserId,
        UpdateReminderPreferenceRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> BuildCalendarInviteAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
