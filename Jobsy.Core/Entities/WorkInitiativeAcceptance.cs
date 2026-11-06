namespace Jobsy.Core.Entities;

public class WorkInitiativeAcceptance
{
    public Guid InitiativeId { get; set; }
    public WorkInitiative Initiative { get; set; } = null!;
    public Guid BillingCompanyId { get; set; }
    public decimal AcceptCostTokens { get; set; }
    public Guid? AcceptSpendTransactionId { get; set; }
    public DateTime AcceptedAtUtc { get; set; } = DateTime.UtcNow;
}
