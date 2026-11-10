namespace Jobsy.Core.Contracts;

/// <summary>
/// Candidate-only coaching tone preferences (self-reported, not scores).
/// Stored in <c>PreferencesJson</c>; never employer-facing.
/// </summary>
public sealed record CoachTonePreferencesDto(
    int Version = 1,
    DateTime? UpdatedAtUtc = null,
    bool Skipped = false,
    IReadOnlyDictionary<string, string[]>? Answers = null);

public sealed record UpdateCoachTonePreferencesRequest(
    IReadOnlyDictionary<string, string[]>? Answers = null,
    bool Wipe = false,
    bool Skipped = false);
