namespace Jobsy.Core.Enums;

/// <summary>
/// Whether the company has proven it is a real organisation on Lobsy.
/// Separate from <see cref="KvkVerificationStatus"/> (KvK record existence).
/// </summary>
public enum CompanyVerificationStatus
{
    Unverified = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3
}
