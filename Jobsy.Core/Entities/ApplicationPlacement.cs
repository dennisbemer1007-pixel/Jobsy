using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Token accept + employment choice for phase-2 employer flows (direct hire vs Maqqie).
/// One row per application once the employer accepts.
/// </summary>
public class ApplicationPlacement
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public Guid BillingCompanyId { get; set; }

    /// <summary>Ledger spend for accept (typically ½ token during pilot).</summary>
    public Guid? AcceptSpendTransactionId { get; set; }
    public TokenTransaction? AcceptSpendTransaction { get; set; }

    public decimal AcceptCostTokens { get; set; }

    /// <summary>Null for staffing-agency accepts (no Maqqie choice).</summary>
    public PlacementEmploymentMode? EmploymentMode { get; set; }
    public DateTime? EmploymentModeChosenAtUtc { get; set; }

    /// <summary>When the ½ token was credited back after Maqqie week 1.</summary>
    public DateTime? MaqqieCreditGrantedAtUtc { get; set; }
    public Guid? MaqqieCreditGrantTransactionId { get; set; }
    public TokenTransaction? MaqqieCreditGrantTransaction { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
