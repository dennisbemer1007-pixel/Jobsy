namespace Jobsy.Core.Enums;

/// <summary>
/// Legacy colleague takeovers remain decidable by the bedrijfsmanager.
/// New ownership transfers require a letter + admin approval (07.5).
/// </summary>
public enum TakeoverRequestKind
{
    /// <summary>Pre-07 employer-to-employer takeover (inbox-decidable).</summary>
    Colleague = 0,

    /// <summary>Ownership transfer: letter to KvK address + admin approval.</summary>
    OwnershipTransfer = 1
}
