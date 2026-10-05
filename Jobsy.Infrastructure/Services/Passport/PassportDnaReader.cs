using Jobsy.Core.Entities;
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

        return
        [
            Layer(
                PassportDnaLayer.Competence,
                competency?.CompletedAtUtc,
                competency is null,
                PassportDnaWords.Competency(competency is null ? null : new CompetencyScores(
                    competency.SamenwerkenPercent,
                    competency.ResultaatgerichtheidPercent,
                    competency.StressbestendigheidPercent,
                    competency.InnovatiePercent,
                    competency.ExtraversiePercent), language)),
            Layer(
                PassportDnaLayer.Career,
                career?.CompletedAtUtc,
                career is null,
                PassportDnaWords.Career(career is null ? null : new RiasecScores(
                    career.RealisticPercent,
                    career.InvestigativePercent,
                    career.ArtisticPercent,
                    career.SocialPercent,
                    career.EnterprisingPercent,
                    career.ConventionalPercent), language)),
            Layer(
                PassportDnaLayer.Culture,
                culture?.CompletedAtUtc,
                culture is null,
                PassportDnaWords.Culture(culture is null ? null : new CulturePersonalityScores(
                    culture.AutonomyPercent,
                    culture.InformalPercent,
                    culture.CollaborationPercent,
                    culture.FlexibilityPercent,
                    culture.InnovationPercent,
                    culture.PeopleFirstPercent), language)),
            Layer(
                PassportDnaLayer.Values,
                values?.CompletedAtUtc,
                values is null,
                PassportDnaWords.Values(values is null ? null : new SchwartzValuesScores(
                    values.AutonomyPercent,
                    values.ConnectionPercent,
                    values.AchievementPercent,
                    values.StabilityPercent,
                    values.ImpactPercent), language))
        ];
    }

    private static PassportDnaLayerFact Layer(
        string key,
        DateTime? completedAtUtc,
        bool missing,
        IReadOnlyList<string> words)
        => new(key, !missing, completedAtUtc, words);
}
