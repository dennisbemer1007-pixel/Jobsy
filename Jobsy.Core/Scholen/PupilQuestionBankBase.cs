using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Shared bank construction for G78 and (from 04) VO. Index-in-world is derived from
/// world grouping in the question list — no hardcoded −15/−30/−45 offsets.
/// Public only because a public bank must inherit a public base; not registered in DI.
/// </summary>
public abstract class PupilQuestionBankBase : IPupilQuestionBank
{
    private readonly Dictionary<string, PupilQuestionItem> _byId;

    protected PupilQuestionBankBase(IReadOnlyList<PupilQuestion> questions, int version)
    {
        Version = version;
        Questions = questions;
        AllItems = BuildItems(questions);
        _byId = AllItems.ToDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public int Version { get; }

    public IReadOnlyList<PupilQuestion> Questions { get; }

    public IReadOnlyList<PupilQuestionItem> AllItems { get; }

    public PupilQuestionItem? GetById(string itemId)
        => _byId.TryGetValue(itemId, out var item) ? item : null;

    public PupilQuestionItem? GetByGlobalIndex(int globalIndex)
        => globalIndex >= 0 && globalIndex < AllItems.Count ? AllItems[globalIndex] : null;

    public IReadOnlyList<PupilQuestion> ForModel(AssessmentKind model)
        => Questions.Where(q => q.Model == model).ToList();

    public IReadOnlyList<CompetencyQuestion> AsCompetencyItems()
        => ForModel(AssessmentKind.Competence)
            .Select(q => new CompetencyQuestion(q.Id, q.Category, q.Reverse, q.TextKey))
            .ToList();

    public IReadOnlyList<CareerQuestion> AsCareerItems()
        => ForModel(AssessmentKind.Career)
            .Select(q => new CareerQuestion(q.Id, q.Category, q.Reverse, q.TextKey))
            .ToList();

    public IReadOnlyList<CompetencyQuestion> AsValuesItems()
        => ForModel(AssessmentKind.Values)
            .Select(q => new CompetencyQuestion(q.Id, q.Category, q.Reverse, q.TextKey))
            .ToList();

    public IReadOnlyList<CompetencyQuestion> AsCultureItems()
        => ForModel(AssessmentKind.Culture)
            .Select(q => new CompetencyQuestion(q.Id, q.Category, q.Reverse, q.TextKey))
            .ToList();

    public static string WorldKey(PupilWorld world) => world switch
    {
        PupilWorld.Koraalrif => "koraalrif",
        PupilWorld.Schatgrot => "schatgrot",
        PupilWorld.Vuurtoren => "vuurtoren",
        PupilWorld.Lagune => "lagune",
        _ => "koraalrif"
    };

    public static string WorldTitle(PupilWorld world) => world switch
    {
        PupilWorld.Koraalrif => "Koraalrif",
        PupilWorld.Schatgrot => "Schatgrot",
        PupilWorld.Vuurtoren => "Vuurtoren",
        PupilWorld.Lagune => "Lagune",
        _ => world.ToString()
    };

    private static IReadOnlyList<PupilQuestionItem> BuildItems(IReadOnlyList<PupilQuestion> questions)
    {
        var indexInWorld = new Dictionary<PupilWorld, int>();
        var items = new List<PupilQuestionItem>(questions.Count);
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            indexInWorld.TryGetValue(q.World, out var worldIndex);
            indexInWorld[q.World] = worldIndex + 1;
            items.Add(new PupilQuestionItem(
                q.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                WorldKey(q.World),
                worldIndex,
                i,
                q.TextKey,
                q.ExampleKey,
                q.Model,
                q.Category,
                q.Reverse,
                q.Id));
        }

        return items;
    }
}
