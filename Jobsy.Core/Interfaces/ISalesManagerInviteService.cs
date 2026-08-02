namespace Jobsy.Core.Interfaces;

public interface ISalesManagerInviteService
{
    /// <summary>
    /// Provisions a salesmanager account. Admin direct invites create recruiting (tier-0) managers.
    /// When <paramref name="referredBySalesManagerUserId"/> is set, the new manager cannot recruit further.
    /// </summary>
    Task<SalesManagerInviteResult> InviteAsync(
        string email,
        string fullName,
        Guid? referredBySalesManagerUserId = null,
        CancellationToken cancellationToken = default);
}

public sealed record SalesManagerInviteResult(
    Guid UserId,
    string Email,
    string FullName,
    string TemporaryPassword,
    bool CreatedNewUser,
    bool CanRecruitSalesManagers,
    Guid? ReferredBySalesManagerUserId);
