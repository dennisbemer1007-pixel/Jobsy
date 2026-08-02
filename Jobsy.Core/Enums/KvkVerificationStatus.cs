namespace Jobsy.Core.Enums;

/// <summary>
/// KVK handelsregister verification state for companies / registrations.
/// Pending = API was unavailable at registration; background job retries later.
/// </summary>
public enum KvkVerificationStatus
{
    /// <summary>Validated against KVK (stub or live API) at registration.</summary>
    Verified = 0,

    /// <summary>Registration continued while KVK API was down; awaiting retry.</summary>
    Pending = 1,

    /// <summary>Retries exhausted or KVK permanently rejected the establishment.</summary>
    Failed = 2
}
