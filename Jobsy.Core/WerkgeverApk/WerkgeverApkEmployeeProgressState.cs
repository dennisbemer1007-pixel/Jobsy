using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Progress helpers using the werkgerichte vragenset (not pupil catalog).</summary>
public static class WerkgeverApkEmployeeProgressState
{
    public static int AnsweredInBlock(
        this PupilMboHboProgressState state,
        PupilMboHboBlock block,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        var prefix = block switch
        {
            PupilMboHboBlock.Interesse => "i",
            PupilMboHboBlock.Competentie => "c",
            PupilMboHboBlock.Werkwaarden => "w",
            _ => ""
        };
        _ = catalog.QuestionCount(block, level);
        return state.Answers.Keys.Count(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }

    public static bool BlockFullyAnswered(
        this PupilMboHboProgressState state,
        PupilMboHboBlock block,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
    {
        var questions = catalog.QuestionsFor(block, level);
        foreach (var q in questions)
        {
            if (!state.Answers.TryGetValue(q.Id, out var v) || !LikertAnswerJson.IsValidAnswer(v))
            {
                return false;
            }
        }

        return questions.Count > 0;
    }
}