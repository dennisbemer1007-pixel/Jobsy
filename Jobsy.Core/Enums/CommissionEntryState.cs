namespace Jobsy.Core.Enums;

/// <summary>Derived wallet state for a commission ledger line (not stored).</summary>
public enum CommissionEntryState
{
    Pending = 0,
    Available = 1,
    Requested = 2,
    Paid = 3,
    Settled = 4
}
