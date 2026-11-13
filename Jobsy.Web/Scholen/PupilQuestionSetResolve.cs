using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Web.Scholen;

internal static class PupilQuestionSetResolve
{
    public static PupilQuestionSetDef ForProgress(IPupilQuestionSetRegistry registry, PupilProgressStateDto progress)
    {
        if (progress.QuestionSet == PupilQuestionSet.Groep78
            && !string.IsNullOrEmpty(progress.QuestionScoringVersion))
        {
            return registry.GetGroep78(progress.QuestionScoringVersion);
        }

        return registry.Get(progress.QuestionSet);
    }
}
