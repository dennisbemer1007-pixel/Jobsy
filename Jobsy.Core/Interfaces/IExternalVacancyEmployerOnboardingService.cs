namespace Jobsy.Core.Interfaces;

public interface IExternalVacancyEmployerOnboardingService
{
    /// <summary>
    /// Resolves the outbound row for a valid invite token when the contact e-mail matches the invite.
    /// </summary>
    Task<Guid?> TryResolveOutboundIdAsync(
        string inviteToken,
        string normalizedContactEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// After employer registration activates: draft vacancy, candidate application, and metric links.
    /// Idempotent when already completed.
    /// </summary>
    Task<ExternalVacancyOnboardingResult> CompleteRegistrationAsync(
        Guid outboundId,
        Guid branchCompanyId,
        Guid employerUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ExternalVacancyOnboardingResult(
    bool Succeeded,
    string? PostActivationWebPath,
    string? ErrorCode);
