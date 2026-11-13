using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>G78 contrast question bank (scoring version g78-2). Same ids 9001–9060 as v1.</summary>
public sealed class PupilQuestionBankG78V2 : PupilQuestionBankBase
{
    public const int ScoringVersion = 2;
    public const string ScoringVersionKey = "g78-2";

    public PupilQuestionBankG78V2()
        : base(BuildQuestions(), ScoringVersion)
    {
    }

    private static IReadOnlyList<PupilQuestion> BuildQuestions()
        =>
        [
            Q(9001, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken),
            Q(9002, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid),
            Q(9003, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid),
            Q(9004, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie),
            Q(9005, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie),
            Q(9006, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken),
            Q(9007, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid),
            Q(9008, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid),
            Q(9009, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie),
            Q(9010, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie),
            Q(9011, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken),
            Q(9012, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid),
            Q(9013, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid),
            Q(9014, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie),
            Q(9015, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie),
            Q(9016, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, Rules.CareerTestCatalog.Social),
            Q(9017, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, Rules.CareerTestCatalog.Enterprising),
            Q(9018, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, Rules.CareerTestCatalog.Conventional),
            Q(9019, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, Rules.CareerTestCatalog.Realistic),
            Q(9020, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, Rules.CareerTestCatalog.Investigative),
            Q(9021, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, Rules.CareerTestCatalog.Artistic),
            Q(9022, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, Rules.CareerTestCatalog.Social),
            Q(9023, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, Rules.CareerTestCatalog.Conventional),
            Q(9024, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, Rules.CareerTestCatalog.Realistic),
            Q(9025, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, Rules.CareerTestCatalog.Enterprising),
            Q(9026, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, Rules.CareerTestCatalog.Investigative),
            Q(9027, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, Rules.CareerTestCatalog.Artistic),
            Q(9028, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, Rules.CareerTestCatalog.Social),
            Q(9029, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, Rules.CareerTestCatalog.Conventional),
            Q(9030, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, Rules.CareerTestCatalog.Realistic),
            Q(9031, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, Rules.SchwartzValuesCatalog.Stability),
            Q(9032, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, Rules.SchwartzValuesCatalog.Achievement),
            Q(9033, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, Rules.SchwartzValuesCatalog.Connection),
            Q(9034, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, Rules.SchwartzValuesCatalog.Autonomy),
            Q(9035, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact),
            Q(9036, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, Rules.SchwartzValuesCatalog.Stability),
            Q(9037, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, Rules.SchwartzValuesCatalog.Achievement),
            Q(9038, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, Rules.SchwartzValuesCatalog.Connection),
            Q(9039, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, Rules.SchwartzValuesCatalog.Autonomy),
            Q(9040, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact),
            Q(9041, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, Rules.SchwartzValuesCatalog.Stability),
            Q(9042, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, Rules.SchwartzValuesCatalog.Achievement),
            Q(9043, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, Rules.SchwartzValuesCatalog.Connection),
            Q(9044, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, Rules.SchwartzValuesCatalog.Autonomy),
            Q(9045, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact),
            Q(9046, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, Rules.CulturePersonalityCatalog.Collaboration),
            Q(9047, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, Rules.CulturePersonalityCatalog.Autonomy),
            Q(9048, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst),
            Q(9049, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal),
            Q(9050, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility),
            Q(9051, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, Rules.CulturePersonalityCatalog.Innovation),
            Q(9052, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, Rules.CulturePersonalityCatalog.Collaboration),
            Q(9053, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, Rules.CulturePersonalityCatalog.Autonomy),
            Q(9054, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst),
            Q(9055, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal),
            Q(9056, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, Rules.CulturePersonalityCatalog.Flexibility),
            Q(9057, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, Rules.CulturePersonalityCatalog.Innovation),
            Q(9058, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, Rules.CulturePersonalityCatalog.Collaboration),
            Q(9059, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, Rules.CulturePersonalityCatalog.Autonomy),
            Q(9060, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst),
        ];

    private static PupilQuestion Q(
        int id,
        PupilWorld world,
        AssessmentKind model,
        string categoryA,
        string? categoryB = null)
        => new(
            id,
            world,
            model,
            categoryA,
            Reverse: false,
            TextKeyA: $"LeerlingQ.{id}.A",
            TextKeyB: $"LeerlingQ.{id}.B",
            ExampleKey: $"LeerlingQ.{id}.Voorbeeld",
            CategoryB: categoryB);
}
