using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.Passport;

/// <summary>Loads completed tests and keeps the percents inside this class.</summary>
public sealed class PassportDnaReader : IPassportDnaReader
{
    private readonly JobsyDbContext _db;

    public PassportDnaReader(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(
        Guid userId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        var culture = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        var values = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.Status == CandidateCompetencyStatuses.Completed,
                cancellationToken);
        var deep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.UserId == userId && d.Status == CandidateDeepAnalysisStatuses.Completed)
            .ToListAsync(cancellationToken);
        var competenceDeep = deep.FirstOrDefault(d => d.Kind == AssessmentKind.Competence);
        var careerDeep = deep.FirstOrDefault(d => d.Kind == AssessmentKind.Career);
        var cultureDeep = deep.FirstOrDefault(d => d.Kind == AssessmentKind.Culture);
        var valuesDeep = deep.FirstOrDefault(d => d.Kind == AssessmentKind.Values);
        var deepTraits = competenceDeep is null
            ? []
            : StoredDeepScores.CompetenceTraits(competenceDeep.Status, competenceDeep.AnswersJson, competenceDeep.ReportJson);
        var deepCompetency = CompetencyFromTraits(deepTraits);
        var careerScores = careerDeep is null
            ? null
            : StoredDeepScores.Career(careerDeep.Status, careerDeep.AnswersJson, careerDeep.ReportJson);
        var cultureScores = cultureDeep is null
            ? null
            : StoredDeepScores.Culture(cultureDeep.Status, cultureDeep.AnswersJson, cultureDeep.ReportJson);
        var valuesScores = valuesDeep is null
            ? null
            : StoredDeepScores.Values(valuesDeep.Status, valuesDeep.AnswersJson, valuesDeep.ReportJson);

        var competenceScores = deepCompetency ?? (competency is null ? null : new CompetencyScores(
            competency.SamenwerkenPercent,
            competency.ResultaatgerichtheidPercent,
            competency.StressbestendigheidPercent,
            competency.InnovatiePercent,
            competency.ExtraversiePercent));
        var careerForItems = careerScores ?? (career is null ? null : new RiasecScores(
            career.RealisticPercent,
            career.InvestigativePercent,
            career.ArtisticPercent,
            career.SocialPercent,
            career.EnterprisingPercent,
            career.ConventionalPercent));
        var cultureForItems = cultureScores ?? (culture is null ? null : new CulturePersonalityScores(
            culture.AutonomyPercent,
            culture.InformalPercent,
            culture.CollaborationPercent,
            culture.FlexibilityPercent,
            culture.InnovationPercent,
            culture.PeopleFirstPercent));
        var valuesForItems = valuesScores ?? (values is null ? null : new SchwartzValuesScores(
            values.AutonomyPercent,
            values.ConnectionPercent,
            values.AchievementPercent,
            values.StabilityPercent,
            values.ImpactPercent));

        var competenceItems = PassportDnaWords.CompetencyItems(competenceScores, language, take: 4);
        var careerItems = PassportDnaWords.CareerItems(careerForItems, language, take: 3);
        var cultureItems = PassportDnaWords.CultureItems(cultureForItems, language, take: 4);
        var valueItems = PassportDnaWords.ValueItems(valuesForItems, language, take: 4);

        return
        [
            Layer(
                PassportDnaLayer.Competence,
                deepCompetency is not null ? competenceDeep!.CompletedAtUtc : competency?.CompletedAtUtc,
                competenceScores is null,
                competenceItems),
            Layer(
                PassportDnaLayer.Career,
                careerScores is not null ? careerDeep!.CompletedAtUtc : career?.CompletedAtUtc,
                careerForItems is null,
                careerItems),
            Layer(
                PassportDnaLayer.Culture,
                cultureScores is not null ? cultureDeep!.CompletedAtUtc : culture?.CompletedAtUtc,
                cultureForItems is null,
                cultureItems),
            Layer(
                PassportDnaLayer.Values,
                valuesScores is not null ? valuesDeep!.CompletedAtUtc : values?.CompletedAtUtc,
                valuesForItems is null,
                valueItems)
        ];
    }

    private static CompetencyScores? CompetencyFromTraits(IReadOnlyList<(string Domain, int Score)> traits)
    {
        if (traits.Count == 0)
        {
            return null;
        }

        int? Score(string code)
        {
            foreach (var trait in traits)
            {
                if (string.Equals(trait.Domain, code, StringComparison.OrdinalIgnoreCase))
                {
                    return trait.Score;
                }
            }

            return null;
        }

        var scores = new CompetencyScores(
            Score(CompetencyTestCatalog.Samenwerken),
            Score(CompetencyTestCatalog.Resultaatgerichtheid),
            Score(CompetencyTestCatalog.Stressbestendigheid),
            Score(CompetencyTestCatalog.Innovatie),
            Score(CompetencyTestCatalog.Extraversie));
        return scores.IsComplete ? scores : null;
    }

    private static PassportDnaLayerFact Layer(
        string key,
        DateTime? completedAtUtc,
        bool missing,
        IReadOnlyList<(string Code, string Label)> items)
        => new(
            key,
            !missing,
            completedAtUtc,
            items.Select(item => item.Label).ToArray(),
            items.Select(item => item.Code).ToArray());
}
