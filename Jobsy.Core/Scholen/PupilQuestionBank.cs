using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

public enum PupilWorld
{
    Koraalrif = 1,
    Schatgrot = 2,
    Vuurtoren = 3,
    Lagune = 4
}

/// <summary>One Likert item in the leerling bank (ids 9001–9060).</summary>
public sealed record PupilQuestion(
    int Id,
    PupilWorld World,
    AssessmentKind Model,
    string Category,
    bool Reverse,
    string TextKey,
    string ExampleKey);

/// <summary>Wizard/API view of a pupil item (string id for JSON answers).</summary>
public sealed record PupilQuestionItem(
    string Id,
    string WorldKey,
    int IndexInWorld,
    int GlobalIndex,
    string TextKey,
    string ImagineKey,
    AssessmentKind Model,
    string Category,
    bool Reverse,
    int NumericId);

public interface IPupilQuestionBank
{
    int Version { get; }

    IReadOnlyList<PupilQuestion> Questions { get; }

    IReadOnlyList<PupilQuestionItem> AllItems { get; }

    PupilQuestionItem? GetById(string itemId);

    PupilQuestionItem? GetByGlobalIndex(int globalIndex);

    IReadOnlyList<PupilQuestion> ForModel(AssessmentKind model);
}

/// <summary>
/// Fixed 60-item bank: 15 per model, interleaved so adjacent items never share a category.
/// Scoring reuses adult catalog math via explicit item overloads.
/// </summary>
public sealed class PupilQuestionBank : PupilQuestionBankBase
{
    public const int ScoringVersion = 1;
    public const int MinId = 9001;
    public const int MaxId = 9060;
    public const int Count = 60;

    public PupilQuestionBank()
        : base(BuildQuestions(), ScoringVersion)
    {
    }

    public static new string WorldKey(PupilWorld world) => PupilQuestionBankBase.WorldKey(world);

    public static new string WorldTitle(PupilWorld world) => PupilQuestionBankBase.WorldTitle(world);

    private static IReadOnlyList<PupilQuestion> BuildQuestions()
    {
        // Display order = fixed order. Interleaved so adjacent items never share a category.
        // Reverse flags follow 05.3 table (values: no reverse even though adult Schwartz has reverse items).
        return
        [
            // —— Koraalrif · Competentietest (Big Five) · 9001–9015 ——
            Q(9001, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9002, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9003, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9004, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9005, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9006, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9007, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9008, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9009, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9010, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9011, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, true),
            Q(9012, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, true),
            Q(9013, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, true),
            Q(9014, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, true),
            Q(9015, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, true),

            // —— Schatgrot · Beroepentest (RIASEC) · 9016–9030 · no reverse ——
            Q(9016, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9017, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9018, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9019, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9020, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9021, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9022, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9023, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9024, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9025, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9026, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9027, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9028, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9029, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9030, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),

            // —— Vuurtoren · Waarden (Schwartz) · 9031–9045 · no reverse (table) ——
            Q(9031, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9032, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9033, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9034, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9035, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9036, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9037, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9038, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9039, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9040, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9041, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9042, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9043, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9044, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9045, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),

            // —— Lagune · Cultuur · 9046–9060 · Informal has 1 reverse ——
            Q(9046, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9047, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9048, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9049, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, false),
            Q(9050, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9051, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
            Q(9052, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9053, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9054, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9055, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, true),
            Q(9056, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9057, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
            Q(9058, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9059, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9060, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
        ];
    }

    private static PupilQuestion Q(
        int id,
        PupilWorld world,
        AssessmentKind model,
        string category,
        bool reverse)
        => new(
            id,
            world,
            model,
            category,
            reverse,
            $"LeerlingQ.{id}",
            $"LeerlingQ.{id}.Voorbeeld");
}

/// <summary>Builds <see cref="Entities.Scholen.PupilResult"/> after all items are answered.</summary>
public interface IPupilResultBuilder
{
    Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default);
}
