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

        var competencyItems = PassportDnaWords.CompetencyItems(
            competency is null ? null : new CompetencyScores(
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent),
            language,
            take: 4);
        var careerItems = PassportDnaWords.CareerItems(
            career is null ? null : new RiasecScores(
                career.RealisticPercent,
                career.InvestigativePercent,
                career.ArtisticPercent,
                career.SocialPercent,
                career.EnterprisingPercent,
                career.ConventionalPercent),
            language,
            take: 3);
        var cultureItems = PassportDnaWords.CultureItems(
            culture is null ? null : new CulturePersonalityScores(
                culture.AutonomyPercent,
                culture.InformalPercent,
                culture.CollaborationPercent,
                culture.FlexibilityPercent,
                culture.InnovationPercent,
                culture.PeopleFirstPercent),
            language,
            take: 4);
        var valueItems = PassportDnaWords.ValueItems(
            values is null ? null : new SchwartzValuesScores(
                values.AutonomyPercent,
                values.ConnectionPercent,
                values.AchievementPercent,
                values.StabilityPercent,
                values.ImpactPercent),
            language,
            take: 4);

        return
        [
            Layer(PassportDnaLayer.Competence, competency?.CompletedAtUtc, competency is null, competencyItems),
            Layer(PassportDnaLayer.Career, career?.CompletedAtUtc, career is null, careerItems),
            Layer(PassportDnaLayer.Culture, culture?.CompletedAtUtc, culture is null, cultureItems),
            Layer(PassportDnaLayer.Values, values?.CompletedAtUtc, values is null, valueItems)
        ];
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
