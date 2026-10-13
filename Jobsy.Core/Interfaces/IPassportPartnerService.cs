using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IPassportPartnerService
{
    Task<PassportCodeSummary?> ResolveCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<PassportLinkMutation> AttachByCodeAsync(
        Guid candidateId,
        string code,
        PassportPartnerLinkSource source,
        CancellationToken cancellationToken = default);

    Task<PassportLinkMutation> GiveConsentAsync(
        Guid candidateId,
        Guid linkId,
        string? consentVersion,
        bool contactConsent,
        bool confirmAdult,
        CancellationToken cancellationToken = default);

    Task<PassportLinkMutation> RevokeAsync(
        Guid candidateId,
        Guid linkId,
        PassportPartnerRevokeReason reason,
        CancellationToken cancellationToken = default);

    Task<PassportLinkMutation> ReconfirmAsync(
        Guid candidateId,
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<PassportPartnerViewGrant?> CanPartnerViewAsync(
        Guid partnerUserId,
        Guid candidateId,
        bool mfaSatisfied,
        CancellationToken cancellationToken = default);

    Task LogAccessAsync(
        Guid candidateId,
        Guid? passportPartnerId,
        Guid? viewerUserId,
        Guid? shareLinkId,
        PassportAccessKind kind,
        CancellationToken cancellationToken = default);
}

public sealed record PassportCodeSummary(
    Guid PartnerId,
    string DisplayName,
    string Type,
    string BranchLabel,
    bool HasLogo);

public sealed record PassportLinkMutation(bool Ok, string? Error, Guid? LinkId);

public sealed record PassportPartnerViewGrant(Guid LinkId, Guid PassportPartnerId);
