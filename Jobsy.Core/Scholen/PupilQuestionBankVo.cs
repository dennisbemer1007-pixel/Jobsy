using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>
/// VO 100-item bank (ids 9101–9200). Texts live in <c>UiStringsLeerlingVragenVo</c>; this file is structure only.
/// </summary>
public sealed class PupilQuestionBankVo : PupilQuestionBankBase
{
    public const int ScoringVersion = 1;
    public const int MinId = 9101;
    public const int MaxId = 9200;
    public const int Count = 100;

    public PupilQuestionBankVo()
        : base(BuildQuestions(), ScoringVersion)
    {
    }

    private static IReadOnlyList<PupilQuestion> BuildQuestions()
        =>
        [
            Q(9101, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9102, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9103, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9104, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9105, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9106, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9107, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, true),
            Q(9108, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9109, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9110, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, true),
            Q(9111, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9112, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9113, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9114, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, true),
            Q(9115, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9116, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, false),
            Q(9117, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9118, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, true),
            Q(9119, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9120, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9121, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Samenwerken, true),
            Q(9122, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Resultaatgerichtheid, false),
            Q(9123, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Stressbestendigheid, false),
            Q(9124, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Innovatie, false),
            Q(9125, PupilWorld.Koraalrif, AssessmentKind.Competence, Rules.CompetencyTestCatalog.Extraversie, false),
            Q(9126, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9127, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9128, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9129, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9130, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9131, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9132, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9133, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9134, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9135, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9136, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9137, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9138, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9139, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9140, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9141, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9142, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9143, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9144, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9145, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Realistic, false),
            Q(9146, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Investigative, false),
            Q(9147, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Artistic, false),
            Q(9148, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Social, false),
            Q(9149, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Enterprising, false),
            Q(9150, PupilWorld.Schatgrot, AssessmentKind.Career, Rules.CareerTestCatalog.Conventional, false),
            Q(9151, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9152, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9153, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9154, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9155, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9156, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9157, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9158, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9159, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9160, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9161, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9162, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9163, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9164, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9165, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9166, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9167, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9168, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9169, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9170, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9171, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Autonomy, false),
            Q(9172, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Connection, false),
            Q(9173, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Achievement, false),
            Q(9174, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Stability, false),
            Q(9175, PupilWorld.Vuurtoren, AssessmentKind.Values, Rules.SchwartzValuesCatalog.Impact, false),
            Q(9176, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9177, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9178, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9179, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9180, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, false),
            Q(9181, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9182, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
            Q(9183, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9184, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9185, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9186, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, false),
            Q(9187, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9188, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
            Q(9189, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9190, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9191, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9192, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, false),
            Q(9193, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9194, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
            Q(9195, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Autonomy, false),
            Q(9196, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Collaboration, false),
            Q(9197, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.PeopleFirst, false),
            Q(9198, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Informal, true),
            Q(9199, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Flexibility, false),
            Q(9200, PupilWorld.Lagune, AssessmentKind.Culture, Rules.CulturePersonalityCatalog.Innovation, false),
        ];

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
