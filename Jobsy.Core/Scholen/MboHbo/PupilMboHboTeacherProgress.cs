using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboTeacherProgress
{
    public const int BlockCount = 5;

    public static (int Current, int Total) ProgressCounts(PupilCode code, SchoolClass schoolClass)
    {
        if (schoolClass.QuestionSet != PupilQuestionSet.MboHbo)
        {
            return (0, 0);
        }

        if (code.Status == PupilCodeStatus.Completed)
        {
            return (BlockCount, BlockCount);
        }

        var state = PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson);
        var done = PupilMboHboCatalog.AllBlocks.Count(state.IsBlockComplete);
        return (done, BlockCount);
    }

    public static bool IsFullyComplete(PupilCode code, SchoolClass schoolClass)
        => schoolClass.QuestionSet == PupilQuestionSet.MboHbo
           && (code.Status == PupilCodeStatus.Completed
               || PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson).AllBlocksComplete());
}
