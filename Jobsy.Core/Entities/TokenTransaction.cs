using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class TokenTransaction
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>
    /// Positive for purchase/grant/in; negative for spend/out.
    /// Supports half-tokens (e.g. highlight = -0.5).
    /// </summary>
    public decimal Amount { get; set; }

    public TokenTransactionKind Kind { get; set; }
    public TokenSpendReason Reason { get; set; } = TokenSpendReason.None;
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public Guid? VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }
    public Guid? BranchCompanyId { get; set; }
    public Company? BranchCompany { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
