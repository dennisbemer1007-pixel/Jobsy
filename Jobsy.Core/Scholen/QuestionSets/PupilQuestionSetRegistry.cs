using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.QuestionSets;

/// <summary>
/// Core registry of pupil tests. No DI dependencies — safe as a singleton in Api and Web.
/// Groep78 (60 items) and VO (100 items) are fully separate; there is no LegacyVo fallback.
/// </summary>
public sealed class PupilQuestionSetRegistry : IPupilQuestionSetRegistry
{
    private readonly Dictionary<PupilQuestionSet, PupilQuestionSetDef> _bySet;

    public PupilQuestionSetRegistry()
    {
        var g78Bank = new PupilQuestionBank();
        var voBank = new PupilQuestionBankVo();
        var g78Worlds = new PupilWorldPlan[]
        {
            new("koraalrif", 15),
            new("schatgrot", 15),
            new("vuurtoren", 15),
            new("lagune", 15),
        };
        var voWorlds = new PupilWorldPlan[]
        {
            new("koraalrif", 25),
            new("schatgrot", 25),
            new("vuurtoren", 25),
            new("lagune", 25),
        };
        var g78Labels = new (int Value, string Key)[]
        {
            (1, "Leerling.Answer.No"),
            (2, "Leerling.Answer.NotReally"),
            (3, "Leerling.Answer.Sometimes"),
            (4, "Leerling.Answer.Quite"),
            (5, "Leerling.Answer.Yes"),
        };
        var voLabels = new (int Value, string Key)[]
        {
            (1, "Leerling.Vo.Answer.1"),
            (2, "Leerling.Vo.Answer.2"),
            (3, "Leerling.Vo.Answer.3"),
            (4, "Leerling.Vo.Answer.4"),
            (5, "Leerling.Vo.Answer.5"),
        };

        var g78 = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Groep78,
            Key: "g78",
            ScoringVersion: "g78-1",
            Bank: g78Bank,
            Worlds: g78Worlds,
            IslandAfter: 30,
            PuzzleSlots: [],
            AnswerLabels: g78Labels,
            ItemsPerPlate: 6,
            PlateCount: 10,
            CheerKeyPrefix: "LeerlingQ.Cheer.",
            CheerCount: 12,
            PartBreakAfterIsland: false,
            StartTimeKey: "Leerling.Start.NoteTime",
            LabelKey: "School.QuestionSet.Groep78");

        var vo = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Vo,
            Key: "vo",
            ScoringVersion: "vo-1",
            Bank: voBank,
            Worlds: voWorlds,
            IslandAfter: 50,
            PuzzleSlots: [],
            AnswerLabels: voLabels,
            ItemsPerPlate: 10,
            PlateCount: 10,
            CheerKeyPrefix: "LeerlingQ.Vo.Cheer.",
            CheerCount: 12,
            PartBreakAfterIsland: true,
            StartTimeKey: "Leerling.Vo.Start.NoteTime",
            LabelKey: "School.QuestionSet.Vo");

        _bySet = new Dictionary<PupilQuestionSet, PupilQuestionSetDef>
        {
            [PupilQuestionSet.Groep78] = g78,
            [PupilQuestionSet.Vo] = vo,
        };
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

    public bool IsLegacy(PupilQuestionSet set)
    {
        _ = set;
        return false;
    }

    public PupilQuestionSetDef? FindByItemId(PupilQuestionSet set, string itemId)
    {
        if (!_bySet.TryGetValue(set, out var def))
        {
            return null;
        }

        return def.Bank.GetById(itemId) is null ? null : def;
    }
}
