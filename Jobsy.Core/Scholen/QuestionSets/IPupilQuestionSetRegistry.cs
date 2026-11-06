using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.QuestionSets;

/// <summary>
/// Resolves the question-set definition for a school class.
/// Pupil-facing Infrastructure code must use <see cref="ForClass"/> — never fall back to the other test.
/// </summary>
public interface IPupilQuestionSetRegistry
{
    /// <summary>The ONLY way pupil Infrastructure code reaches a def (= <see cref="Get"/>(schoolClass.QuestionSet)).</summary>
    PupilQuestionSetDef ForClass(SchoolClass schoolClass);

    /// <summary>Throws for unknown values; never falls back to the other test.</summary>
    PupilQuestionSetDef Get(PupilQuestionSet set);

    /// <summary>Always false from 04: LegacyVo is gone. Kept so callers do not need a second cut-over.</summary>
    bool IsLegacy(PupilQuestionSet set);

    /// <summary>Returns the def when <paramref name="itemId"/> belongs to that test; otherwise null.</summary>
    PupilQuestionSetDef? FindByItemId(PupilQuestionSet set, string itemId);

    IReadOnlyList<PupilQuestionSetDef> All { get; }
}
