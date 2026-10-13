namespace Jobsy.Core.Scholen;

/// <summary>Template keys stored on <see cref="Entities.Scholen.PupilResult.StoryKeysJson"/> (not rendered text).</summary>
public sealed record PupilStoryKeySet(
    int Version,
    string BigFiveKey,
    string RiasecKey,
    string SchwartzKey,
    string CultureKey,
    IReadOnlyList<string> LikeChipKeys,
    string TileCompetenceKey,
    string TileRiasecKey,
    string TileValueKey,
    string TileCultureKey,
    IReadOnlyList<string> JobIdeaKeys);

/// <summary>Fit snapshot keys + counts for teacher aggregates (no prose).</summary>
public sealed record PupilFitSnapshot(
    string JobKey,
    int HaveCount,
    int TotalCount,
    IReadOnlyList<string> NeedKeys,
    IReadOnlyList<bool> Met);
