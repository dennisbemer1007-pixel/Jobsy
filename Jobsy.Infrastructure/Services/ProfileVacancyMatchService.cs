using System.Collections.Concurrent;
using System.Security.Claims;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class ProfileVacancyMatchService : IProfileVacancyMatchService
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;
    private readonly ICompanyCultureLookup _companyCulture;
    private readonly IPlatformFeatureService _features;
    private readonly CandidateMatchScoreCache _matchCache;
    private readonly ILogger<ProfileVacancyMatchService> _logger;

    public ProfileVacancyMatchService(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users,
        ICompanyCultureLookup companyCulture,
        IPlatformFeatureService features,
        CandidateMatchScoreCache matchCache,
        ILogger<ProfileVacancyMatchService> logger)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
        _companyCulture = companyCulture;
        _features = features;
        _matchCache = matchCache;
        _logger = logger;
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

    public async Task<IReadOnlyDictionary<Guid, ProfileVacancyMatch>> ScoreAsync(
        ProfileVacancyMatchContext context,
        IEnumerable<(VacancyDiscoveryRecord Record, int? TravelMinutes)> vacancies,
        CancellationToken cancellationToken = default)
    {
        var batch = vacancies as IList<(VacancyDiscoveryRecord Record, int? TravelMinutes)>
                    ?? vacancies.ToList();
        if (batch.Count == 0)
        {
            return new Dictionary<Guid, ProfileVacancyMatch>();
        }

        var platform = await _features.GetAsync(cancellationToken);
        var useCalibrationV1 = platform.MatchCalibrationV1Enabled;
        var employerFit = platform.EmployerFitQuestionnairesEnabled;
        var result = new Dictionary<Guid, ProfileVacancyMatch>(batch.Count);
        var toScore = new List<(VacancyDiscoveryRecord Record, int? TravelMinutes)>();
        foreach (var item in batch)
        {
            if (_matchCache.TryGet(
                    context.UserId,
                    context.ProfileRevision,
                    useCalibrationV1,
                    employerFit,
                    item.Record.Id,
                    out var cached)
                && cached is not null)
            {
                result[item.Record.Id] = cached;
            }
            else
            {
                toScore.Add(item);
            }
        }

        if (toScore.Count == 0)
        {
            return result;
        }

        var companyIds = toScore.Select(v => v.Record.CompanyId).Distinct().ToList();
        var companyProfiles = await _companyCulture.GetForCompaniesAsync(companyIds, cancellationToken);
        var scored = new ConcurrentDictionary<Guid, ProfileVacancyMatch>();
        Parallel.ForEach(
            toScore,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 8)
            },
            item =>
            {
                var match = CalculateMatch(
                    context,
                    item.Record,
                    item.TravelMinutes,
                    companyProfiles,
                    useCalibrationV1,
                    employerFit);
                scored[item.Record.Id] = match;
                if (useCalibrationV1 && match.LegacyTotalPercent is int legacy)
                {
                    _logger.LogDebug(
                        "MatchCalibrationV1 compare vacancy={VacancyId} legacy={Legacy} fit={Fit} confidence={Confidence} display={Display}",
                        item.Record.Id,
                        legacy,
                        match.FitRawPercent,
                        match.ConfidencePercent,
                        match.TotalPercent);
                }
            });

        _matchCache.PutMany(context.UserId, context.ProfileRevision, useCalibrationV1, employerFit, scored);
        foreach (var kv in scored)
        {
            result[kv.Key] = kv.Value;
        }

        return result;
    }

    private static ProfileVacancyMatch CalculateMatch(
        ProfileVacancyMatchContext context,
        VacancyDiscoveryRecord record,
        int? travelMinutes,
        IReadOnlyDictionary<Guid, CompanyCultureLookupResult> companyProfiles,
        bool useCalibrationV1,
        bool employerFit)
    {
        companyProfiles.TryGetValue(record.CompanyId, out var companyProfile);
        var overridesCulture = CulturePillarCatalog.HasProfile(record.CulturePillars);
        var core = MatchingProfileMapper.BuildInput(record, context.Prefs, travelMinutes, context.AgeYears);
        var extras = employerFit
            ? VacancyMatchProfileResolver.ParseExtras(
                record.VacancyWorkContextJson,
                record.VacancyCompetencyTargetsJson,
                record.VacancyCultureDeltaJson,
                record.VacancyUseInferredCompetencies)
            : VacancyMatchExtrasSnapshot.Empty;
        var resolved = employerFit
            ? VacancyMatchProfileResolver.Resolve(
                record.WorkTypeLabelList,
                record.Title,
                record.Description,
                extras,
                overridesCulture ? null : companyProfile?.Culture,
                record.CulturePillars)
            : null;
        var matchLabels = SensitiveExperienceRules.MatchingExperienceLabels(context.Prefs);
        var mergedRoles = (context.Prefs.Roles ?? Array.Empty<string>())
            .Concat(matchLabels)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return ProfileVacancyMatchCalculator.Calculate(new ProfileVacancyMatchInput
        {
            VacancyId = record.Id,
            Core = core,
            VacancyTitle = record.Title,
            VacancyDescription = record.Description,
            WorkTypes = record.WorkTypeLabelList,
            CandidateRoles = mergedRoles,
            CandidateLicenses = context.Prefs.DrivingLicenses,
            CandidateEducations = context.Prefs.Educations,
            CandidateEmployerCount = SensitiveExperienceRules.CountForMinimumEmployers(context.Prefs),
            RequiredDrivingLicense = record.RequiredDrivingLicense,
            RequiredEducation = record.RequiredEducation,
            MinimumEmployers = record.MinimumEmployers,
            CandidateCompetencies = context.Competencies,
            VacancyCompetencies = resolved?.VacancyCompetencies
                                  ?? record.InferredCompetencies
                                  ?? VacancyCompetencyProfile.Infer(record),
            CandidateRiasecTags = context.RiasecTags,
            CandidateRiasecScores = context.RiasecScores,
            CareerDeepCompleted = context.CareerDeepCompleted,
            VacancyRiasecTags = record.InferredRiasecTags ?? VacancyRiasecProfile.InferTags(record),
            CareerOccupations = context.CareerOccupations,
            CulturePillars = record.CulturePillars,
            CandidateCultureScores = context.CultureScores,
            CompanyCultureScores = resolved?.EffectiveCulture ?? (overridesCulture ? null : companyProfile?.Culture),
            CandidateValuesScores = context.ValuesScores,
            CompanyValuesScores = companyProfile?.Values,
            CompanyEngagement = companyProfile?.Engagement,
            CompanyName = record.CompanyName,
            CompetencyMatchEvidence = context.CompetencyMatchEvidence,
            CareerMatchEvidence = context.CareerMatchEvidence,
            CandidateWorkContext = context.CandidateWorkContext,
            VacancyWorkContext = employerFit ? resolved?.WorkContext : null,
            VacancyCompetenciesFromEmployer = resolved?.VacancyCompetenciesExplicit ?? false,
            VacancyCultureFromEmployer = resolved?.CultureExplicit ?? false,
            VacancyWorkContextFromEmployer = employerFit && resolved?.WorkContext is not null,
            EmployerFitQuestionnairesEnabled = employerFit
        }, useCalibrationV1);
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

        var competencyVersion = AssessmentTestVersions.Normalize(competency?.QuickScanVersion);
        var competencyResolved = competency is null
            ? new ProvisionalAssessmentScores.ResolvedCompetency(null, false)
            : ProvisionalAssessmentScores.ResolveCompetency(
                competency.Status,
                competency.AnswersJson,
                competency.SamenwerkenPercent,
                competency.ResultaatgerichtheidPercent,
                competency.StressbestendigheidPercent,
                competency.InnovatiePercent,
                competency.ExtraversiePercent,
                competencyVersion);
        var scores = competencyResolved.Scores is { IsComplete: true } ? competencyResolved.Scores : null;

        var careerVersion = AssessmentTestVersions.Normalize(career?.QuickScanVersion);
        var careerResolved = career is null
            ? new ProvisionalAssessmentScores.ResolvedRiasec(null, false)
            : ProvisionalAssessmentScores.ResolveCareer(
                career.Status,
                career.AnswersJson,
                career.RealisticPercent,
                career.InvestigativePercent,
                career.ArtisticPercent,
                career.SocialPercent,
                career.EnterprisingPercent,
                career.ConventionalPercent,
                careerVersion);
        var riasecScores = careerResolved.Scores is { IsComplete: true } ? careerResolved.Scores : null;
        var riasecTags = riasecScores is not null
            ? CareerTestCatalog.DeriveRiasecTags(riasecScores)
            : career is not null && CandidateCompetencyStatuses.IsCompleted(career.Status)
                ? CareerTestCatalog.ParseTagsJson(career.RiasecTagsJson)
                : Array.Empty<string>();
        var careerDeep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId
                     && d.Kind == AssessmentKind.Career
                     && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);

        var occupations = ResolveOccupations(career, riasecScores, careerDeep);

        var cultureRow = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        CulturePersonalityScores? storedCulture = null;
        if (cultureRow is not null && CandidateCompetencyStatuses.IsCompleted(cultureRow.Status))
        {
            storedCulture = new CulturePersonalityScores(
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
        }

        var cultureVersion = AssessmentTestVersions.Normalize(cultureRow?.QuickScanVersion);
        var cultureResolved = ProvisionalAssessmentScores.ResolveCulture(
            cultureRow?.Status, cultureRow?.AnswersJson, storedCulture, cultureVersion);
        var culture = cultureResolved.Scores is { IsComplete: true } ? cultureResolved.Scores : null;

        var valuesRow = await _db.CandidateValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        var valuesResolved = valuesRow is null
            ? new ProvisionalAssessmentScores.ResolvedValues(null, false)
            : ProvisionalAssessmentScores.ResolveValues(
                valuesRow.Status,
                valuesRow.AnswersJson,
                valuesRow.AutonomyPercent,
                valuesRow.ConnectionPercent,
                valuesRow.AchievementPercent,
                valuesRow.StabilityPercent,
                valuesRow.ImpactPercent,
                AssessmentTestVersions.Normalize(valuesRow.QuickScanVersion));
        var values = valuesResolved.Scores is { IsComplete: true } ? valuesResolved.Scores : null;

        var competenceDeep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .AnyAsync(
                d => d.UserId == userId
                     && d.Kind == AssessmentKind.Competence
                     && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);

        var competencyEvidence = ResolveQuickScanEvidence(
            AssessmentKind.Competence,
            competencyVersion,
            competency?.QuickScanHighestAnswered ?? 0,
            scores is { IsComplete: true },
            competencyResolved.IsProvisional,
            competenceDeep);
        var careerEvidence = ResolveQuickScanEvidence(
            AssessmentKind.Career,
            careerVersion,
            career?.QuickScanHighestAnswered ?? 0,
            riasecScores is { IsComplete: true },
            careerResolved.IsProvisional,
            careerDeep);

        var candidateWorkContext = CandidateWorkContextProfile.FromPreferences(prefs.WorkPreferences);
        var profileRevision = CandidateMatchProfileRevision.Compute(
            user,
            prefs,
            competency,
            career,
            cultureRow,
            valuesRow);

        return new ProfileVacancyMatchContext
        {
            UserId = user.Id,
            HomeLatitude = user.HomeLocation?.Latitude,
            HomeLongitude = user.HomeLocation?.Longitude,
            Prefs = prefs,
            CandidateWorkContext = candidateWorkContext,
            AgeYears = AgeRules.AgeYearsFromDateOfBirth(user.DateOfBirth) ?? prefs.AgeYears,
            Competencies = scores,
            RiasecTags = riasecTags,
            RiasecScores = riasecScores,
            CareerDeepCompleted = careerDeep,
            CareerOccupations = occupations,
            CultureScores = culture,
            ValuesScores = values,
            IsProvisional = competencyResolved.IsProvisional
                            || careerResolved.IsProvisional
                            || cultureResolved.IsProvisional
                            || valuesResolved.IsProvisional,
            CompetencyMatchEvidence = competencyEvidence,
            CareerMatchEvidence = careerEvidence,
            OpenForWork = user.OpenForWork,
            ProfileRevision = profileRevision
        };
    }

    private static AssessmentMatchEvidence ResolveQuickScanEvidence(
        AssessmentKind kind,
        int version,
        int highestAnswered,
        bool scoresComplete,
        bool isProvisional,
        bool deepCompleted)
    {
        if (deepCompleted)
        {
            return AssessmentMatchEvidence.DeepAnalysis;
        }

        return AssessmentMatchEvidenceRules.FromQuickScan(
            scoresComplete,
            highestAnswered,
            AssessmentQuickScanCatalog.QuestionCount(kind, version),
            isProvisional);
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
