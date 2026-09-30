using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Renders "Dit ben jij" story + fit tiles from template keys (file 06: <see cref="PupilStoryRenderer"/>).
/// </summary>
public interface IPupilStoryRenderer
{
    PupilStoryViewDto Render(PupilResult result, PupilProgress? progress);

    DreamJobRouteStubDto RenderDreamRoute(PupilResult result, PupilProgress? progress);

    /// <summary>Resolved Dutch starter lines (not localization keys).</summary>
    IReadOnlyList<string> ConversationStarterKeys(PupilResult result);

    /// <summary>Resolved Dutch group prompts for the class top-2 RIASEC letters.</summary>
    IReadOnlyList<string> ClassDiscussionPromptKeys(string? topLetter1 = null, string? topLetter2 = null);
}
