using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.QuestionSets;

/// <summary>
/// Core registry of pupil tests. No DI dependencies — safe as a singleton in Api and Web.
/// In 03a Vo maps to the interim <c>LegacyVo</c> def (same 60 items as today).
/// </summary>
public sealed class PupilQuestionSetRegistry : IPupilQuestionSetRegistry
{
    private readonly Dictionary<PupilQuestionSet, PupilQuestionSetDef> _bySet;
    private readonly HashSet<PupilQuestionSet> _legacy;

    public PupilQuestionSetRegistry()
    {
        var g78Bank = new PupilQuestionBank();
        var legacyVoBank = new PupilQuestionBank();
        var worlds = new PupilWorldPlan[]
        {
            new("koraalrif", 15),
            new("schatgrot", 15),
            new("vuurtoren", 15),
            new("lagune", 15),
        };
        var labels = new (int Value, string Key)[]
        {
            (1, "Leerling.Answer.No"),
            (2, "Leerling.Answer.NotReally"),
            (3, "Leerling.Answer.Sometimes"),
            (4, "Leerling.Answer.Quite"),
            (5, "Leerling.Answer.Yes"),
        };

        var g78 = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Groep78,
            Key: "g78",
            ScoringVersion: "g78-1",
            Bank: g78Bank,
            Worlds: worlds,
            IslandAfter: 30,
            PuzzleSlots: [],
            AnswerLabels: labels,
            ItemsPerPlate: 6,
            PlateCount: 10,
            CheerKeyPrefix: "LeerlingQ.Cheer.",
            CheerCount: 12,
            PartBreakAfterIsland: false,
            StartTimeKey: "Leerling.Start.NoteTime",
            LabelKey: "School.QuestionSet.Groep78");

        // Interim until 04: existing VO classes keep today's 60-item behaviour.
        var legacyVo = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Vo,
            Key: "legacy-vo",
            ScoringVersion: "1",
            Bank: legacyVoBank,
            Worlds: worlds,
            IslandAfter: 30,
            PuzzleSlots: [],
            AnswerLabels: labels,
            ItemsPerPlate: 6,
            PlateCount: 10,
            CheerKeyPrefix: "LeerlingQ.Cheer.",
            CheerCount: 12,
            PartBreakAfterIsland: false,
            StartTimeKey: "Leerling.Start.NoteTime",
            LabelKey: "School.QuestionSet.Vo");

        _bySet = new Dictionary<PupilQuestionSet, PupilQuestionSetDef>
        {
            [PupilQuestionSet.Groep78] = g78,
            [PupilQuestionSet.Vo] = legacyVo,
        };
        _legacy = [PupilQuestionSet.Vo];
        All = _bySet.Values.ToList();
    }

    public IReadOnlyList<PupilQuestionSetDef> All { get; }

    public PupilQuestionSetDef ForClass(SchoolClass schoolClass)
    {
        ArgumentNullException.ThrowIfNull(schoolClass);
        return Get(schoolClass.QuestionSet);
    }

    public PupilQuestionSetDef Get(PupilQuestionSet set)
    {
        if (_bySet.TryGetValue(set, out var def))
        {
            return def;
        }

        throw new ArgumentOutOfRangeException(nameof(set), set,
            "Unknown pupil question set — no fallback to another test.");
    }

    public bool IsLegacy(PupilQuestionSet set) => _legacy.Contains(set);

    public PupilQuestionSetDef? FindByItemId(PupilQuestionSet set, string itemId)
    {
        if (!_bySet.TryGetValue(set, out var def))
        {
            return null;
        }

        return def.Bank.GetById(itemId) is null ? null : def;
    }
}
