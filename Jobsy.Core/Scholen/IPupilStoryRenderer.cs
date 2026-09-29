using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Renders "Dit ben jij" story + fit tiles from template keys.
/// File 06 replaces <see cref="StubPupilStoryRenderer"/> with the real template catalog.
/// </summary>
public interface IPupilStoryRenderer
{
    PupilStoryViewDto Render(PupilResult result, PupilProgress? progress);

    DreamJobRouteStubDto RenderDreamRoute(PupilResult result, PupilProgress? progress);

    IReadOnlyList<string> ConversationStarterKeys(PupilResult result);

    IReadOnlyList<string> ClassDiscussionPromptKeys();
}

/// <summary>Placeholder renderer so 03 can ship UI without the 06 catalog.</summary>
public sealed class StubPupilStoryRenderer : IPupilStoryRenderer
{
    public const string PlaceholderBodyKey = "Leraar.Detail.StoryPlaceholder";

    public PupilStoryViewDto Render(PupilResult result, PupilProgress? progress)
    {
        ArgumentNullException.ThrowIfNull(result);
        var holland = string.IsNullOrWhiteSpace(result.HollandCode) ? "—" : result.HollandCode.Trim().ToUpperInvariant();
        var topValue = string.IsNullOrWhiteSpace(result.TopValue) ? "—" : result.TopValue.Trim();
        var topCulture = string.IsNullOrWhiteSpace(result.TopCulture) ? "—" : result.TopCulture.Trim();

        return new PupilStoryViewDto(
            Title: "Dit ben jij",
            Body: PlaceholderBodyKey,
            Tiles:
            [
                new PupilStoryTileDto("competence", "Zo ben jij", "Uitkomst volgt in het verhaal."),
                new PupilStoryTileDto("riasec", "Dit doe je graag", holland),
                new PupilStoryTileDto("values", "Dit vind je belangrijk", topValue),
                new PupilStoryTileDto("culture", "Hier voel je je thuis", topCulture),
            ]);
    }

    public DreamJobRouteStubDto RenderDreamRoute(PupilResult result, PupilProgress? progress)
    {
        ArgumentNullException.ThrowIfNull(result);
        var key = result.DreamJobKey ?? progress?.DreamJobKey;
        return new DreamJobRouteStubDto(
            JobKey: key,
            JobTitle: key,
            HaveCount: 0,
            TotalCount: 5,
            HaveItems: [],
            LearnItems: [],
            RouteSteps: ["Nu", "Volgende stap", "Opleiding", "Beroep"]);
    }

    public IReadOnlyList<string> ConversationStarterKeys(PupilResult result) =>
    [
        "Leraar.Detail.Starter1",
        "Leraar.Detail.Starter2",
        "Leraar.Detail.Starter3",
    ];

    public IReadOnlyList<string> ClassDiscussionPromptKeys() =>
    [
        "Leraar.Group.Prompt1",
        "Leraar.Group.Prompt2",
        "Leraar.Group.Prompt3",
    ];
}
