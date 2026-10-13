namespace Jobsy.Web.Components.Candidate.Journey;

/// <summary>State of one stone on the climb (§S).</summary>
public enum ClimbStoneState
{
    /// <summary>Passed: the old shell shard stays behind.</summary>
    Done,

    /// <summary>The stone the lobster stands on.</summary>
    Now,

    /// <summary>Still to come.</summary>
    Todo,

    /// <summary>The golden dream stone in the light.</summary>
    Dream
}

/// <summary>One stone with its HTML label (the SVG itself is aria-hidden).</summary>
/// <param name="ShortMark">Mobile hero mark inside the stone (Nu / 1…n); null for the dream star.</param>
public sealed record ClimbStone(
    string Label,
    ClimbStoneState State,
    string? StateText = null,
    string? ShortMark = null);

/// <summary>Scene pose (§S).</summary>
public enum ClimbVariant
{
    /// <summary>The lobster climbs; trail gold up to it, dotted beyond.</summary>
    Climb,

    /// <summary>Empty state: the lobster listens with its antenna towards the dream stone.</summary>
    Listen,

    /// <summary>Stones only, no lobster.</summary>
    Stones
}
