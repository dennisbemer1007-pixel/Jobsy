namespace Jobsy.Core.Enums;

/// <summary>How a company reached its current <see cref="CompanyVerificationStatus"/>.</summary>
public enum CompanyVerificationMethod
{
    None = 0,
    BusinessEmail = 1,
    Letter = 2,
    Manual = 3,
    Backfill = 4,
    AdminCreated = 5,
    InheritedFromOrganization = 6,
    IntermediaryClient = 7
}
