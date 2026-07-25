namespace Jobsy.Core.Enums;

public enum RegistrationScope
{
    BranchOnly = 0,
    Organization = 1
}

public enum CompanyRegistrationStatus
{
    PendingActivation = 0,
    Activated = 1,
    TakeoverPending = 2,
    TakeoverApproved = 3,
    TakeoverRejected = 4,
    Cancelled = 5
}

public enum TakeoverRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}
