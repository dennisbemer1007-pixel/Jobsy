namespace Jobsy.Core.Enums;

/// <summary>Lifecycle of a scraped ATS listing before/after admin moderation.</summary>
public enum AtsListingStatus
{
    PendingReview = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3,
    Inactive = 4
}
