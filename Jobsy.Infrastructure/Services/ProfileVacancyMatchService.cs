using System.Security.Claims;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class ProfileVacancyMatchService : IProfileVacancyMatchService
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public ProfileVacancyMatchService(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
    }

    public async Task<ProfileVacancyMatchContext?> TryLoadForPrincipalAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!_companyAuth.IsCandidate(user))
        {
            return null;
        }

        var entity = await _users.FindByPrincipalAsync(user, cancellationToken);
        return entity is null ? null : await LoadForUserAsync(entity.Id, cancellationToken);
    }

    public Task<ProfileVacancyMatchContext?> TryLoadForUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => LoadForUserAsync(userId, cancellationToken);

    public IReadOnlyDictionary<Guid, ProfileVacancyMatch> Score(
        ProfileVacancyMatchContext context,
        IEnumerable<(VacancyDiscoveryRecord Record, int? TravelMinutes)> vacancies)
    {
        var result = new Dictionary<Guid, ProfileVacancyMatch>();
        foreach (var (record, travelMinutes) in vacancies)
        {
            var core = MatchingProfileMapper.BuildInput(record, context.Prefs, travelMinutes, context.AgeYears);
            var match = ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
            {
                VacancyId = record.Id,
                Core = core,
                VacancyTitle = record.Title,
                VacancyDescription = record.Description,
                WorkTypes = record.WorkTypeLabelList,
                CandidateRoles = context.Prefs.Roles,
                CandidateLicenses = context.Prefs.DrivingLicenses,
                CandidateEducations = context.Prefs.Educations,
                CandidateEmployerCount = context.Prefs.Employers?.Count ?? 0,
                RequiredDrivingLicense = record.RequiredDrivingLicense,
                RequiredEducation = record.RequiredEducation,
                MinimumEmployers = record.MinimumEmployers,
                CandidateCompetencies = context.Competencies,
                VacancyCompetencies = VacancyCompetencyProfile.Infer(record),
                CandidateRiasecTags = context.RiasecTags,
                CandidateRiasecScores = context.RiasecScores,
                CareerDeepCompleted = context.CareerDeepCompleted,
                VacancyRiasecTags = VacancyRiasecProfile.InferTags(record),
                CareerOccupations = context.CareerOccupations,
                CulturePillars = record.CulturePillars,
                CandidateCultureScores = context.CultureScores,
                CompanyCultureScores = context.CompanyCultureScores
            });
            result[record.Id] = match;
        }

        return result;
    }

    private async Task<ProfileVacancyMatchContext?> LoadForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var prefs = MatchingProfileMapper.DeserializePrefs(user.PreferencesJson);
        var competency = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var career = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        var scores = competency is null
            ? null
            : CompetencyTestCatalog.CompletedScoresOrNull(
                competency.Status,
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent);

        var riasecTags = career is not null && CandidateCompetencyStatuses.IsCompleted(career.Status)
            ? CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson)
            : Array.Empty<string>();
        var riasecScores = career is null
            ? null
            : CareerTestCatalog.CompletedScoresOrNull(
                career.Status,
                career.RealisticPercent,
                career.InvestigativePercent,
                career.ArtisticPercent,
                career.SocialPercent,
                career.EnterprisingPercent,
                career.ConventionalPercent);
        var careerDeep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId
                     && d.Kind == AssessmentKind.Career
                     && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);

        var occupations = ResolveOccupations(career, riasecScores, careerDeep);

        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        CulturePersonalityScores? culture = null;
        if (cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status))
        {
            culture = new CulturePersonalityScores(
                cultureRow.AutonomyPercent,
                cultureRow.InformalPercent,
                cultureRow.CollaborationPercent,
                cultureRow.FlexibilityPercent,
                cultureRow.InnovationPercent,
                cultureRow.PeopleFirstPercent,
                cultureRow.OpennessPercent,
                cultureRow.ConscientiousnessPercent,
                cultureRow.ExtraversionPercent,
                cultureRow.AgreeablenessPercent,
                cultureRow.EmotionalStabilityPercent);
            if (culture is not { IsComplete: true })
            {
                culture = null;
            }
        }

        return new ProfileVacancyMatchContext
        {
            UserId = user.Id,
            HomeLatitude = user.HomeLocation?.Latitude,
            HomeLongitude = user.HomeLocation?.Longitude,
            Prefs = prefs,
            AgeYears = AgeRules.AgeYearsFromDateOfBirth(user.DateOfBirth) ?? prefs.AgeYears,
            Competencies = scores,
            RiasecTags = riasecTags,
            RiasecScores = riasecScores,
            CareerDeepCompleted = careerDeep,
            CareerOccupations = occupations,
            CultureScores = culture
        };
    }

    private static IReadOnlyList<CareerOccupationMatch> ResolveOccupations(
        CandidateCareerInterest? career,
        RiasecScores? riasecScores,
        bool careerDeep)
    {
        var stored = CareerCompassJson.TryDeserialize(career?.CompassJson);
        if (stored is { HasOccupations: true })
        {
            return stored.AllOccupations.ToList();
        }

        return CareerCompassBuilder.Build(riasecScores, careerDeep).AllOccupations.ToList();
    }
}
