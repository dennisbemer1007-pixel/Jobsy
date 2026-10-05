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
        var competenceWords = deepTraits.Count > 0
            ? deepTraits.Take(2).Select(trait => DimensionLabels.For(trait.Domain, language)).ToArray()
            : PassportDnaWords.Competency(competency is null ? null : new CompetencyScores(
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent), language);
        var careerScores = careerDeep is null
            ? null
            : StoredDeepScores.Career(careerDeep.Status, careerDeep.AnswersJson, careerDeep.ReportJson);
        var cultureScores = cultureDeep is null
            ? null
            : StoredDeepScores.Culture(cultureDeep.Status, cultureDeep.AnswersJson, cultureDeep.ReportJson);
        var valuesScores = valuesDeep is null
            ? null
            : StoredDeepScores.Values(valuesDeep.Status, valuesDeep.AnswersJson, valuesDeep.ReportJson);

        return
        [
            Layer(
                PassportDnaLayer.Competence,
                competenceDeep?.CompletedAtUtc ?? competency?.CompletedAtUtc,
                competency is null && competenceDeep is null,
                competenceWords),
            Layer(
                PassportDnaLayer.Career,
                careerDeep?.CompletedAtUtc ?? career?.CompletedAtUtc,
                career is null && careerDeep is null,
                PassportDnaWords.Career(careerScores ?? (career is null ? null : new RiasecScores(
                    career.RealisticPercent,
                    career.InvestigativePercent,
                    career.ArtisticPercent,
                    career.SocialPercent,
                    career.EnterprisingPercent,
                    career.ConventionalPercent)), language)),
            Layer(
                PassportDnaLayer.Culture,
                cultureDeep?.CompletedAtUtc ?? culture?.CompletedAtUtc,
                culture is null && cultureDeep is null,
                PassportDnaWords.Culture(cultureScores ?? (culture is null ? null : new CulturePersonalityScores(
                    culture.AutonomyPercent,
                    culture.InformalPercent,
                    culture.CollaborationPercent,
                    culture.FlexibilityPercent,
                    culture.InnovationPercent,
                    culture.PeopleFirstPercent)), language)),
            Layer(
                PassportDnaLayer.Values,
                valuesDeep?.CompletedAtUtc ?? values?.CompletedAtUtc,
                values is null && valuesDeep is null,
                PassportDnaWords.Values(valuesScores ?? (values is null ? null : new SchwartzValuesScores(
                    values.AutonomyPercent,
                    values.ConnectionPercent,
                    values.AchievementPercent,
                    values.StabilityPercent,
                    values.ImpactPercent)), language))
        ];
    }

    private static PassportDnaLayerFact Layer(
        string key,
        DateTime? completedAtUtc,
        bool missing,
        IReadOnlyList<string> words)
        => new(key, !missing, completedAtUtc, words);
}
