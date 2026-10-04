using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCareerPlanService : ICandidateCareerPlanService
{
    private const int MaxArchivedPlans = 3;
    private const int ArchiveRetentionDays = 30;

    private static readonly JsonSerializerOptions PrefsJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ICareerPathPlanGenerationService _generate;
    private readonly ICandidateInsightsQueue _insightsQueue;
    private readonly CareerGenerationGuard _guard;
    private readonly ICandidateCompetencyService _competencies;
    private readonly ICandidateCareerInterestService _career;
    private readonly ICandidateCulturePersonalityService _culture;
    private readonly IFeatureFlags _flags;

    public CandidateCareerPlanService(
        JobsyDbContext db,
        ICareerPathPlanGenerationService generate,
        ICandidateInsightsQueue insightsQueue,
        CareerGenerationGuard guard,
        ICandidateCompetencyService competencies,
        ICandidateCareerInterestService career,
        ICandidateCulturePersonalityService culture,
        IFeatureFlags flags)
    {
        _db = db;
        _generate = generate;
        _insightsQueue = insightsQueue;
        _guard = guard;
        _competencies = competencies;
        _career = career;
        _culture = culture;
        _flags = flags;
    }

    public async Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await ActivePlanQuery(userId).FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task SaveDreamAsync(Guid userId, string? dreamTitle, CancellationToken cancellationToken = default)
    {
        var dream = (dreamTitle ?? "").Trim();
        if (dream.Length > 80)
        {
            dream = dream[..80];
        }

        // "Weet ik nog niet" and empty are allowed — store as-is for resume; generation skips blanks.
        var now = DateTime.UtcNow;
        var existing = await ActivePlanQuery(userId, includeProgress: false).FirstOrDefaultAsync(cancellationToken);
        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(dream))
            {
                return;
            }

            _db.CandidateCareerPlans.Add(new CandidateCareerPlan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DreamTitle = dream,
                DreamKey = CareerStepKey.NormalizeDreamKey(dream),
                DreamCatalogKey = CatalogKeyFor(dream),
                PlanJson = "[]",
                MatchPercent = 0,
                MatchSummary = "",
                Status = CareerPlanStatuses.Active,
                PlanLanguage = "nl",
                DreamSource = CareerDreamSources.Wizard,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.DreamTitle = string.IsNullOrWhiteSpace(dream) ? existing.DreamTitle : dream;
            existing.DreamKey = CareerStepKey.NormalizeDreamKey(existing.DreamTitle);
            existing.DreamCatalogKey = CatalogKeyFor(existing.DreamTitle);
            existing.UpdatedAtUtc = now;
            // Keep existing steps; wizard save must not regenerate.
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(dream))
        {
            _insightsQueue.TryEnqueue(userId);
        }
    }

    private static string? CatalogKeyFor(string dream)
        => CareerDreamCatalog.FindByTitleOrAlias(dream)?.Key;

    public async Task<string?> GetDreamTitleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var title = await ActivePlanQuery(userId, includeProgress: false)
            .Select(p => p.DreamTitle)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    public Task EnqueueGenerationIfNeededAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _insightsQueue.TryEnqueue(userId);
        return Task.CompletedTask;
    }

    public async Task TryGeneratePendingAsync(
        Guid userId,
        HorizonCareerProfileSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var plan = await ActivePlanQuery(userId, includeProgress: false).FirstOrDefaultAsync(cancellationToken);
        if (plan is null || string.IsNullOrWhiteSpace(plan.DreamTitle))
        {
            return;
        }

        if (string.Equals(plan.DreamTitle.Trim(), "Weet ik nog niet", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var steps = CareerPlanJson.Deserialize(plan.PlanJson);
        if (steps.Count > 0)
        {
            return;
        }

        try
        {
            await GenerateAndSaveCoreAsync(
                userId,
                plan.DreamTitle,
                snapshot,
                catalogKey: null,
                dreamSource: plan.DreamSource,
                planLanguage: plan.PlanLanguage,
                force: false,
                enforceDailyLimit: false,
                cancellationToken);
        }
        catch (CareerPlanException)
        {
            // System-initiated: in-flight lock just means another request is already generating.
        }
    }

    public Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
        Guid userId,
        string? dreamTitle,
        HorizonCareerProfileSnapshot snapshot,
        string? catalogKey = null,
        string? dreamSource = null,
        string? planLanguage = null,
        bool force = false,
        CancellationToken cancellationToken = default)
        => GenerateAndSaveCoreAsync(
            userId,
            dreamTitle,
            snapshot,
            catalogKey,
            dreamSource,
            planLanguage,
            force,
            enforceDailyLimit: true,
            cancellationToken);

    private async Task<HorizonCareerPathPlanView> GenerateAndSaveCoreAsync(
        Guid userId,
        string? dreamTitle,
        HorizonCareerProfileSnapshot snapshot,
        string? catalogKey,
        string? dreamSource,
        string? planLanguage,
        bool force,
        bool enforceDailyLimit,
        CancellationToken cancellationToken)
    {
        string dream;
        string source;
        string? resolvedCatalogKey = null;
        if (!string.IsNullOrWhiteSpace(catalogKey))
        {
            var entry = CareerDreamCatalog.FindByKey(catalogKey);
            if (entry is null)
            {
                throw new CareerPlanException(CareerPlanErrorCodes.DreamTextInvalid);
            }

            dream = entry.Title;
            resolvedCatalogKey = entry.Key;
            source = CareerDreamSources.Catalog;
        }
        else
        {
            dream = (dreamTitle ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dream))
            {
                throw new CareerPlanException(CareerPlanErrorCodes.DreamTextInvalid);
            }

            source = dreamSource ?? CareerDreamSources.Wizard;
        }

        var language = string.IsNullOrWhiteSpace(planLanguage) ? "nl" : planLanguage.Trim();
        var dreamKey = CareerStepKey.NormalizeDreamKey(dream);

        var existing = await ActivePlanQuery(userId).FirstOrDefaultAsync(cancellationToken);
        var check = await _guard.CheckAsync(
            userId,
            dreamKey,
            existing?.DreamKey,
            existing?.CreatedAtUtc,
            force,
            enforceDailyLimit,
            cancellationToken);

        if (!check.Allowed)
        {
            throw new CareerPlanException(check.Code ?? CareerPlanErrorCodes.GenerationInProgress)
            {
                RetryAfterUtc = check.RetryAfterUtc
            };
        }

        if (check.Reused && existing is not null)
        {
            return await MaterializeAsync(existing, persistAuto: true, cancellationToken);
        }

        var generation = await _guard.BeginAsync(userId, dreamKey, cancellationToken);

        HorizonCareerPathPlanView result;
        try
        {
            var certificates = await LoadCertificatesAsync(userId, cancellationToken);
            var generated = await _generate.GenerateAsync(dream, snapshot, language, cancellationToken);
            var plan = CareerPlanJson.WithStableKeys(generated.Plan);
            var storedSteps = CareerPlanJson.FromGenerated(plan);
            var now = DateTime.UtcNow;

            await using var tx = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(cancellationToken)
                : null;

            IReadOnlyList<CareerPlanCarryOver.OldStep> archivedSteps = [];
            IReadOnlyList<CareerPlanCarryOver.OldProgress> archivedProgress = [];
            if (existing is not null)
            {
                var oldStored = CareerPlanJson.Deserialize(existing.PlanJson);
                archivedSteps = oldStored
                    .Select(s => new CareerPlanCarryOver.OldStep(s.StepKey, s.Order, s.Title, s.Courses))
                    .ToList();
                archivedProgress = existing.StepProgress
                    .Select(p => new CareerPlanCarryOver.OldProgress(p.StepKey, p.CompletedAtUtc, p.Source))
                    .ToList();

                existing.Status = CareerPlanStatuses.Archived;
                existing.ArchivedAtUtc = now;
                existing.UpdatedAtUtc = now;
            }

            var newPlan = new CandidateCareerPlan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DreamTitle = plan.DreamTitle,
                DreamKey = dreamKey,
                PlanJson = CareerPlanJson.Serialize(storedSteps),
                MatchPercent = plan.MatchPercent,
                MatchSummary = plan.MatchSummary,
                Status = CareerPlanStatuses.Active,
                PlanLanguage = language,
                FromAi = generated.FromAi,
                DreamSource = source,
                DreamCatalogKey = resolvedCatalogKey,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.CandidateCareerPlans.Add(newPlan);
            await _db.SaveChangesAsync(cancellationToken);

            var carryNewSteps = storedSteps
                .Select(s => new CareerPlanCarryOver.NewStep(s.StepKey, s.Order, s.Title, s.Courses))
                .ToList();
            var carry = CareerPlanCarryOver.Apply(carryNewSteps, archivedSteps, archivedProgress, certificates);
            foreach (var carried in carry.Carried)
            {
                _db.CandidateCareerStepProgress.Add(new CandidateCareerStepProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PlanId = newPlan.Id,
                    StepKey = carried.StepKey,
                    StepOrder = carried.Order,
                    CompletedAtUtc = carried.CompletedAtUtc,
                    Source = CareerStepProgressSources.CarriedOver,
                    UpdatedAtUtc = now
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await EnforceArchiveCapAsync(userId, cancellationToken);
            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            await _guard.CompleteAsync(generation, CareerGenerationOutcomes.Ok, cancellationToken);

            var reloaded = await ActivePlanQuery(userId).FirstAsync(cancellationToken);
            result = await MaterializeAsync(reloaded, persistAuto: true, cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                await _guard.CompleteAsync(generation, CareerGenerationOutcomes.Failed, CancellationToken.None);
            }
            catch
            {
                // Best effort: never mask the original failure with a guard bookkeeping error.
            }

            throw;
        }

        return result;
    }

    public async Task<HorizonCareerPathPlanView?> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await ActivePlanQuery(userId).FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var steps = CareerPlanJson.Deserialize(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        if (step is null)
        {
            throw new CareerPlanException(CareerPlanErrorCodes.StepNotFound);
        }

        var certificates = await LoadCertificatesAsync(userId, cancellationToken);
        var resolved = ResolveSteps(steps, plan.StepProgress, certificates);
        var target = resolved.FirstOrDefault(r => string.Equals(r.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        if (target is { Status: HorizonCareerStepKind.Completed })
        {
            // Idempotent.
            return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
        }

        var isActive = target?.Status == HorizonCareerStepKind.Active;
        if (!isActive)
        {
            throw new CareerPlanException(CareerPlanErrorCodes.CompletePreviousFirst);
        }

        UpsertProgress(plan, userId, step.StepKey, step.Order, completed: true, CareerStepProgressSources.Manual, undoFingerprint: null);
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task<HorizonCareerPathPlanView?> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await ActivePlanQuery(userId).FirstOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var steps = CareerPlanJson.Deserialize(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        if (step is null)
        {
            throw new CareerPlanException(CareerPlanErrorCodes.StepNotFound);
        }

        var certificates = await LoadCertificatesAsync(userId, cancellationToken);
        var resolved = ResolveSteps(steps, plan.StepProgress, certificates);
        var lastCompleted = resolved
            .Where(r => r.Status == HorizonCareerStepKind.Completed)
            .OrderByDescending(r => r.Order)
            .FirstOrDefault();
        if (lastCompleted is null || !string.Equals(lastCompleted.StepKey, stepKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new CareerPlanException(CareerPlanErrorCodes.UndoLastFirst);
        }

        var fingerprint = CareerStepStatusResolver.FingerprintCertificates(step.Courses, certificates);
        UpsertProgress(plan, userId, step.StepKey, step.Order, completed: false, CareerStepProgressSources.ManualUndo, fingerprint);
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task<IReadOnlyList<ArchivedCareerPlanView>> ListArchivedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var archived = await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .Where(p => p.UserId == userId && p.Status == CareerPlanStatuses.Archived)
            .OrderByDescending(p => p.ArchivedAtUtc)
            .Take(MaxArchivedPlans)
            .ToListAsync(cancellationToken);

        return archived.Select(p =>
        {
            var totalSteps = CareerPlanJson.Deserialize(p.PlanJson).Count;
            var completedSteps = p.StepProgress.Count(s =>
                s.CompletedAtUtc is not null && CareerStepProgressSources.IsCompletionStamp(s.Source));
            var archivedAt = p.ArchivedAtUtc ?? p.UpdatedAtUtc;
            return new ArchivedCareerPlanView(
                p.Id,
                p.DreamTitle,
                archivedAt,
                completedSteps,
                totalSteps,
                archivedAt.AddDays(ArchiveRetentionDays));
        }).ToList();
    }

    public async Task<HorizonCareerPathPlanView?> RestoreArchivedAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var target = await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .FirstOrDefaultAsync(
                p => p.Id == planId && p.UserId == userId && p.Status == CareerPlanStatuses.Archived,
                cancellationToken);
        if (target is null)
        {
            return null;
        }

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var now = DateTime.UtcNow;

        var currentActive = await ActivePlanQuery(userId, includeProgress: false).FirstOrDefaultAsync(cancellationToken);
        if (currentActive is not null)
        {
            currentActive.Status = CareerPlanStatuses.Archived;
            currentActive.ArchivedAtUtc = now;
            currentActive.UpdatedAtUtc = now;
        }

        target.Status = CareerPlanStatuses.Active;
        target.ArchivedAtUtc = null;
        target.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);
        await EnforceArchiveCapAsync(userId, cancellationToken);
        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }

        return await MaterializeAsync(target, persistAuto: true, cancellationToken);
    }

    public async Task<CareerDreamOptionsView> GetDreamOptionsAsync(
        Guid userId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? "").Trim();
        if (q.Length >= 2)
        {
            var results = new List<CareerDreamOptionView>();
            foreach (var entry in CareerDreamCatalog.Search(q, 8))
            {
                results.Add(new CareerDreamOptionView(entry.Key, entry.Title, null));
            }

            if (results.Count < 8)
            {
                var take = 8 - results.Count;
                var folded = CareerOccupationKeys.Fold(q);
                var categories = await _db.VacancyCategories.AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => c.Name)
                    .ToListAsync(cancellationToken);
                foreach (var name in categories)
                {
                    if (results.Count >= 8)
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(name)
                        || results.Any(r => string.Equals(r.Title, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (CareerOccupationKeys.Fold(name).Contains(folded, StringComparison.Ordinal))
                    {
                        results.Add(new CareerDreamOptionView(null, name, null));
                    }
                }

                _ = take;
            }

            return new CareerDreamOptionsView([], results);
        }

        var suggestions = new List<CareerDreamOptionView>();
        var interest = await _db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.CompassJson,
                c.Status,
                c.RealisticPercent,
                c.InvestigativePercent,
                c.ArtisticPercent,
                c.SocialPercent,
                c.EnterprisingPercent,
                c.ConventionalPercent
            })
            .FirstOrDefaultAsync(cancellationToken);
        var compass = CareerCompassJson.TryDeserialize(interest?.CompassJson);
        if (compass is { FromDeepAnalysis: true })
        {
            var scores = CareerTestCatalog.CompletedScoresOrNull(
                interest!.Status,
                interest.RealisticPercent,
                interest.InvestigativePercent,
                interest.ArtisticPercent,
                interest.SocialPercent,
                interest.EnterprisingPercent,
                interest.ConventionalPercent);
            compass = CareerCompassSanitize.EnsureDepth(compass, scores);
        }

        if (compass is not null)
        {
            foreach (var match in compass.AllOccupations.OrderByDescending(m => m.Percent))
            {
                if (suggestions.Count >= 3)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(match.Title))
                {
                    continue;
                }

                var catalogEntry = CareerDreamCatalog.FindByTitleOrAlias(match.Title);
                if (suggestions.Any(s => string.Equals(s.Title, catalogEntry?.Title ?? match.Title, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                suggestions.Add(new CareerDreamOptionView(
                    catalogEntry?.Key,
                    catalogEntry?.Title ?? match.Title,
                    "CareerDream.Reason.Test"));
            }
        }

        return new CareerDreamOptionsView(suggestions, []);
    }

    private async Task EnforceArchiveCapAsync(Guid userId, CancellationToken cancellationToken)
    {
        var archived = await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .Where(p => p.UserId == userId && p.Status == CareerPlanStatuses.Archived)
            .OrderByDescending(p => p.ArchivedAtUtc)
            .ToListAsync(cancellationToken);

        if (archived.Count <= MaxArchivedPlans)
        {
            return;
        }

        var toDelete = archived.Skip(MaxArchivedPlans).ToList();
        foreach (var plan in toDelete)
        {
            _db.CandidateCareerStepProgress.RemoveRange(plan.StepProgress);
            _db.CandidateCareerPlans.Remove(plan);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<CandidateCareerPlan> ActivePlanQuery(Guid userId, bool includeProgress = true)
    {
        var query = _db.CandidateCareerPlans.Where(p => p.UserId == userId && p.Status == CareerPlanStatuses.Active);
        return includeProgress ? query.Include(p => p.StepProgress) : query;
    }

    private static IReadOnlyList<CareerStepStatusResolver.ResolvedStep> ResolveSteps(
        IReadOnlyList<CareerPlanJson.StoredStep> steps,
        IEnumerable<CandidateCareerStepProgress> progress,
        IReadOnlyList<CandidateCertificateDto> certificates)
    {
        var signals = progress
            .Select(p => new CareerStepStatusResolver.ProgressSignal(p.StepKey, p.CompletedAtUtc, p.Source, p.UndoFingerprint))
            .ToList();
        var inputs = steps
            .Select(s => new CareerStepStatusResolver.StepInput(s.StepKey, s.Order, s.Courses.ToList()))
            .ToList();
        return CareerStepStatusResolver.Resolve(inputs, signals, certificates);
    }

    private async Task<HorizonCareerPathPlanView> MaterializeAsync(
        CandidateCareerPlan plan,
        bool persistAuto,
        CancellationToken cancellationToken)
    {
        var stored = CareerPlanJson.Deserialize(plan.PlanJson);
        var certificates = await LoadCertificatesAsync(plan.UserId, cancellationToken);
        var resolved = ResolveSteps(stored, plan.StepProgress, certificates);

        if (persistAuto)
        {
            var dirty = false;
            foreach (var step in resolved.Where(s => s.AutoCompletable))
            {
                UpsertProgress(plan, plan.UserId, step.StepKey, step.Order, completed: true, CareerStepProgressSources.Auto, undoFingerprint: null);
                dirty = true;
            }

            if (dirty)
            {
                plan.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                resolved = ResolveSteps(stored, plan.StepProgress, certificates);
            }
        }

        var (competencyScores, careerScores, cultureScores) = await LoadFitScoresAsync(plan.UserId, cancellationToken);
        var employersEnabled = (await _flags.GetAsync(cancellationToken)).EmployersEnabled;

        string BandFor(string title)
        {
            if (competencyScores is null || careerScores is null)
            {
                return CareerFitBandRules.ToApi(CareerFitBandRules.CareerFitBand.Unknown);
            }

            var snapshot = RoleFitCheckBuilder.Build(title, competencyScores, careerScores, fromDeepAnalysis: false, cultureScores);
            return CareerFitBandRules.ToApi(CareerFitBandRules.From(snapshot.MatchPercent));
        }

        var byKey = resolved.ToDictionary(s => s.StepKey, StringComparer.OrdinalIgnoreCase);
        var activeKey = resolved.FirstOrDefault(s => s.Status == HorizonCareerStepKind.Active)?.StepKey;
        var lastCompletedKey = resolved
            .Where(s => s.Status == HorizonCareerStepKind.Completed)
            .OrderByDescending(s => s.Order)
            .FirstOrDefault()?.StepKey;

        var steps = stored.OrderBy(s => s.Order).Select(s =>
        {
            byKey.TryGetValue(s.StepKey, out var r);
            var status = r?.Status ?? HorizonCareerStepKind.Open;
            var matched = r?.MatchedCourseCount ?? 0;
            var heldBack = r?.HeldBack ?? false;
            var isActive = string.Equals(s.StepKey, activeKey, StringComparison.OrdinalIgnoreCase);
            var isLastCompleted = string.Equals(s.StepKey, lastCompletedKey, StringComparison.OrdinalIgnoreCase);
            var courses = s.Courses
                .Select(c => new HorizonCareerCourseView(c, CareerCourseMatcher.IsOnProfile(c, certificates)))
                .ToList();

            var actions = new List<string>();
            if (s.Courses.Count > 0)
            {
                actions.Add(CareerStepActionNames.ToApi(CareerStepActionKind.Courses));
            }

            if (isActive)
            {
                actions.Add(CareerStepActionNames.ToApi(CareerStepActionKind.AddProof));
            }

            if (employersEnabled)
            {
                actions.Add(CareerStepActionNames.ToApi(CareerStepActionKind.Vacancies));
            }

            if (isActive)
            {
                actions.Add(CareerStepActionNames.ToApi(CareerStepActionKind.Complete));
            }

            if (isLastCompleted)
            {
                actions.Add(CareerStepActionNames.ToApi(CareerStepActionKind.Undo));
            }

            return new HorizonCareerPathStepView(
                s.StepKey,
                s.Order,
                s.Title,
                status.ToString(),
                s.Summary,
                s.SkillsGap.ToList(),
                courses,
                s.MinRequirements.ToList(),
                s.YearsExperienceNeeded,
                BandFor(s.Title),
                heldBack,
                matched,
                actions);
        }).ToList();

        var carriedOverCount = plan.StepProgress.Count(p =>
            string.Equals(p.Source, CareerStepProgressSources.CarriedOver, StringComparison.OrdinalIgnoreCase));

        return new HorizonCareerPathPlanView(
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            CareerStepStatusResolver.GoalReached(resolved),
            plan.FromAi,
            plan.PlanLanguage,
            BandFor(plan.DreamTitle),
            carriedOverCount,
            steps);
    }

    private async Task<(CompetencyScores? Competency, RiasecScores? Career, CulturePersonalityScores? Culture)> LoadFitScoresAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var competency = await _competencies.GetCompletedScoresAsync(userId, cancellationToken);
        var career = await _career.GetCompletedScoresAsync(userId, cancellationToken);
        var culture = await _culture.GetCompletedScoresAsync(userId, cancellationToken);
        return (competency, career, culture);
    }

    private void UpsertProgress(
        CandidateCareerPlan plan,
        Guid userId,
        string stepKey,
        int order,
        bool completed,
        string source,
        string? undoFingerprint)
    {
        var row = plan.StepProgress.FirstOrDefault(p =>
            string.Equals(p.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCareerStepProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                StepKey = stepKey,
                StepOrder = order
            };
            plan.StepProgress.Add(row);
            _db.CandidateCareerStepProgress.Add(row);
        }

        row.StepOrder = order;
        row.Source = source;
        row.CompletedAtUtc = completed ? now : null;
        row.UndoFingerprint = undoFingerprint;
        row.UpdatedAtUtc = now;
    }

    private async Task<IReadOnlyList<CandidateCertificateDto>> LoadCertificatesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var json = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        return ParsePreferences(json).Certificates ?? [];
    }

    private static CandidatePreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, PrefsJson)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }
}
