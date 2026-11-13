using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.Scholen.QuestionSets;

/// <summary>
/// Core registry of pupil tests. No DI dependencies — safe as a singleton in Api and Web.
/// Groep78 (60 items) and VO (100 items) are fully separate; there is no LegacyVo fallback.
/// </summary>
public sealed class PupilQuestionSetRegistry : IPupilQuestionSetRegistry
{
    private readonly Dictionary<PupilQuestionSet, PupilQuestionSetDef> _bySet;
    private readonly PupilQuestionSetDef _g78V1;
    private readonly PupilQuestionSetDef _g78V2;

    public PupilQuestionSetRegistry()
    {
        var g78Bank = new PupilQuestionBank();
        var g78BankV2 = new PupilQuestionBankG78V2();
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
            (5, "Leerling.G78.Answer.Yes"),
        };
        var g78ContrastLabels = new (int Value, string Key)[]
        {
            (1, "Leerling.G78.Contrast.Answer.1"),
            (2, "Leerling.G78.Contrast.Answer.2"),
            (3, "Leerling.G78.Contrast.Answer.3"),
            (4, "Leerling.G78.Contrast.Answer.4"),
            (5, "Leerling.G78.Contrast.Answer.5"),
        };
        var voLabels = new (int Value, string Key)[]
        {
            (1, "Leerling.Vo.Answer.1"),
            (2, "Leerling.Vo.Answer.2"),
            (3, "Leerling.Vo.Answer.3"),
            (4, "Leerling.Vo.Answer.4"),
            (5, "Leerling.Vo.Answer.5"),
        };

        _g78V1 = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Groep78,
            Key: "g78",
            ScoringVersion: PupilG78ScoringVersions.V1,
            Bank: g78Bank,
            Worlds: g78Worlds,
            IslandAfter: 30,
            PuzzleSlots:
            [
                new PupilPuzzleSlot(15, "schelpenrij"),
                new PupilPuzzleSlot(30, "schatkaart"),
                new PupilPuzzleSlot(45, "vuurtorenlampen"),
            ],
            AnswerLabels: g78Labels,
            ItemsPerPlate: 6,
            PlateCount: 10,
            CheerKeyPrefix: "LeerlingQ.Cheer.",
            CheerCount: 12,
            PartBreakAfterIsland: false,
            StartTimeKey: "Leerling.Start.NoteTime",
            LabelKey: "School.QuestionSet.Groep78");

        _g78V2 = new PupilQuestionSetDef(
            Set: PupilQuestionSet.Groep78,
            Key: "g78",
            ScoringVersion: PupilG78ScoringVersions.V2,
            Bank: g78BankV2,
            Worlds: g78Worlds,
            IslandAfter: 30,
            PuzzleSlots:
            [
                new PupilPuzzleSlot(15, "schelpenrij"),
                new PupilPuzzleSlot(30, "schatkaart"),
                new PupilPuzzleSlot(45, "vuurtorenlampen"),
            ],
            AnswerLabels: g78ContrastLabels,
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

        var mboLabels = new (int Value, string Key)[]
        {
            (1, "Leerling.MboHbo.Answer.1"),
            (2, "Leerling.MboHbo.Answer.2"),
            (3, "Leerling.MboHbo.Answer.3"),
            (4, "Leerling.MboHbo.Answer.4"),
            (5, "Leerling.MboHbo.Answer.5"),
        };
        var mboHbo = new PupilQuestionSetDef(
            Set: PupilQuestionSet.MboHbo,
            Key: "mbo-hbo",
            ScoringVersion: PupilMboHboCatalog.ScoringVersion,
            Bank: new PupilQuestionBankMboHbo(),
            Worlds: [],
            IslandAfter: int.MaxValue,
            PuzzleSlots: [],
            AnswerLabels: mboLabels,
            ItemsPerPlate: 1,
            PlateCount: 1,
            CheerKeyPrefix: "Leerling.MboHbo.Cheer.",
            CheerCount: 1,
            PartBreakAfterIsland: false,
            StartTimeKey: "Leerling.MboHbo.Start.NoteTime",
            LabelKey: "School.QuestionSet.MboHbo");

        _bySet = new Dictionary<PupilQuestionSet, PupilQuestionSetDef>
        {
            [PupilQuestionSet.Groep78] = _g78V1,
            [PupilQuestionSet.Vo] = vo,
            [PupilQuestionSet.MboHbo] = mboHbo,
        };
        All = _bySet.Values.ToList();
    }

    public IReadOnlyList<PupilQuestionSetDef> All { get; }

    public PupilQuestionSetDef ForClass(SchoolClass schoolClass, string? g78ScoringVersion = null)
    {
        ArgumentNullException.ThrowIfNull(schoolClass);
        if (schoolClass.QuestionSet == PupilQuestionSet.Groep78 && !string.IsNullOrEmpty(g78ScoringVersion))
        {
            return GetGroep78(g78ScoringVersion);
        }

        return Get(schoolClass.QuestionSet);
    }

    public PupilQuestionSetDef GetGroep78(string scoringVersion)
        => string.Equals(scoringVersion, PupilG78ScoringVersions.V2, StringComparison.Ordinal)
            ? _g78V2
            : _g78V1;

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
        if (set != PupilQuestionSet.Groep78)
        {
            if (!_bySet.TryGetValue(set, out var single))
            {
                return null;
            }

            return single.Bank.GetById(itemId) is null ? null : single;
        }

        if (_g78V1.Bank.GetById(itemId) is not null)
        {
            return _g78V1;
        }

        if (_g78V2.Bank.GetById(itemId) is not null)
        {
            return _g78V2;
        }

        return null;
    }
}
