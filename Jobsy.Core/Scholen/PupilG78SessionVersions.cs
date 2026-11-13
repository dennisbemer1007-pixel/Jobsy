using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen;

public static class PupilG78SessionVersions
{
    public static string Resolve(PupilCode code, bool contrastQuestionsEnabled)
    {
        var pinned = code.Progress?.QuestionScoringVersion;
        if (string.IsNullOrEmpty(pinned))
        {
            pinned = code.Result?.ScoringVersion;
        }

        return PupilG78ScoringVersions.NormalizeOrDefault(pinned, contrastQuestionsEnabled);
    }

    public static void PinIfEmpty(PupilProgress progress, string scoringVersion)
    {
        if (string.IsNullOrEmpty(progress.QuestionScoringVersion)
            && PupilG78ScoringVersions.IsKnown(scoringVersion))
        {
            progress.QuestionScoringVersion = scoringVersion;
        }
    }
}
