using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Request to take over a vestiging that is already registered on Jobsy.
/// </summary>
public class EstablishmentTakeoverRequest
{
    public Guid Id { get; set; }

    public Guid RegistrationId { get; set; }
    public CompanyRegistration Registration { get; set; } = null!;

    public Guid TargetCompanyId { get; set; }
    public Company TargetCompany { get; set; } = null!;

    public TakeoverRequestStatus Status { get; set; } = TakeoverRequestStatus.Pending;

    public Guid? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
}
