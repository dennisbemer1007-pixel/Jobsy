using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

public enum PupilPassportVariant
{
    Groep78,
    Vo
}

public sealed record PupilPassportWorldStamp(
    string Title,
    string Subtitle,
    string Value,
    int WorldNumber,
    string ToneKey);

public sealed record PupilPassportStrength(string Title, string Subtitle, string IconKey);

public sealed record PupilPassportOccupationCard(
    int Rank,
    string DomainLabel,
    string Title,
    string Blurb,
    IReadOnlyList<string> FitTags,
    string IconKey);

public sealed record PupilPassportRouteStep(string Title, string Detail, bool IsGoal);

public sealed record PupilPassportSideJobType(
    string Title,
    string Subtitle,
    IReadOnlyList<string> MetaPills,
    string Why);

public sealed record PupilPassportSectorFit(
    int Rank,
    string Title,
    string Subtitle,
    IReadOnlyList<string> Bullets,
    IReadOnlyList<string> ExampleRoles);

public sealed record PupilPassportRiasecBar(string Letter, string Label, int Score);

public sealed record PupilPassportCultureSlider(string Label, int Position);

public sealed record PupilG78BeroepssoortCard(int Rank, string Key, string Title, string Omdat);

public sealed record PupilG78SchoolDirectionCard(string Key, string Title, string Idea);

public sealed record PupilPassportPdfModel(
    PupilPassportVariant Variant,
    string Language,
    string SchoolName,
    string ClassLabel,
    string DisplayCode,
    DateOnly Date,
    int WorldsCompleted,
    int WorldsTotal,
    /// <summary>Write-in only; never persisted (D3).</summary>
    string? DisplayNameOnPdf,
    string StoryBody,
    IReadOnlyList<PupilPassportWorldStamp> Worlds,
    string? DreamJobTitle,
    string? DreamJobKey,
    int DreamHaveCount,
    int DreamTotalCount,
    IReadOnlyList<string> DreamHaveItems,
    IReadOnlyList<string> DreamLearnItems,
    IReadOnlyList<PupilPassportRouteStep> RouteSteps,
    string? AltRouteNote,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    string? LikeOwnWord,
    string? DislikeOwnWord,
    IReadOnlyList<PupilPassportStrength> Strengths,
    IReadOnlyList<PupilPassportOccupationCard> OccupationCards,
    IReadOnlyList<string> DiscoveryQuestions,
    IReadOnlyList<string> HomeClassTips,
    string FooterPrivacy,
    // VO-only (optional blocks hidden when empty)
    string? ArchetypeTagline,
    string? LevelLine,
    string? ProfileLine,
    string? LocationLine,
    IReadOnlyList<string>? FavoriteSubjects,
    string? SideJobAvailabilityLine,
    string? TravelLine,
    IReadOnlyList<PupilPassportSideJobType>? SideJobTypes,
    IReadOnlyList<PupilPassportSectorFit>? SectorFits,
    IReadOnlyList<string>? LearningGoals,
    string? OwnWords,
    IReadOnlyList<(string Label, string Value)> DnaTiles,
    string? LevelInfoSubline = null,
    string? FavoriteSubjectsSubline = null,
    string? SideJobInfoSubline = null,
    string? TravelInfoSubline = null,
    // G78 bijlagen (optional)
    string? MirrorBody = null,
    IReadOnlyList<PupilPassportRiasecBar>? RiasecBars = null,
    IReadOnlyList<PupilPassportCultureSlider>? CultureSliders = null,
    IReadOnlyList<PupilPassportSubjectGroup>? SubjectGroups = null,
    IReadOnlyList<PupilG78BeroepssoortCard>? BeroepssoortRanks = null,
    IReadOnlyList<PupilG78SchoolDirectionCard>? SchoolDirections = null,
    IReadOnlyList<string>? TopJobKeys = null,
    string? SamenAssignment = null,
    IReadOnlyList<string>? UsageParentTips = null,
    IReadOnlyList<string>? UsageTeacherTips = null);
