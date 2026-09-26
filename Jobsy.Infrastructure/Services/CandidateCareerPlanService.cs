using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCareerPlanService : ICandidateCareerPlanService
{
    private static readonly JsonSerializerOptions PrefsJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly ICareerPathPlanGenerationService _generate;
    private readonly ICandidateInsightsQueue _insightsQueue;

    public CandidateCareerPlanService(
        JobsyDbContext db,
        ICareerPathPlanGenerationService generate,
        ICandidateInsightsQueue insightsQueue)
    {
        _db = db;
        _generate = generate;
        _insightsQueue = insightsQueue;
    }

    public async Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
        Guid userId,
        string dreamTitle,
        HorizonCareerProfileSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var dream = (dreamTitle ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dream))
        {
            throw new InvalidOperationException("Vul een stip op de horizon in.");
        }

        var generated = await _generate.GenerateAsync(dream, snapshot, cancellationToken);
        generated = CareerPlanJson.WithStableKeys(generated);
        var storedSteps = CareerPlanJson.FromGenerated(generated);
        var now = DateTime.UtcNow;

        var existing = await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            _db.CandidateCareerStepProgress.RemoveRange(existing.StepProgress);
            _db.CandidateCareerPlans.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var plan = new CandidateCareerPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DreamTitle = generated.DreamTitle,
            DreamKey = CareerStepKey.NormalizeDreamKey(generated.DreamTitle),
            PlanJson = CareerPlanJson.Serialize(storedSteps),
            MatchPercent = generated.MatchPercent,
            MatchSummary = generated.MatchSummary,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _db.CandidateCareerPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task<HorizonCareerPathPlanView?> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await RequirePlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var steps = CareerPlanJson.Deserialize(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        if (step is null)
        {
            throw new InvalidOperationException("Stap niet gevonden in je plan.");
        }

        UpsertProgress(plan, userId, step.StepKey, step.Order, completed: true, CareerStepProgressSources.Manual);
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    public async Task<HorizonCareerPathPlanView?> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await RequirePlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var steps = CareerPlanJson.Deserialize(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
        if (step is null)
        {
            throw new InvalidOperationException("Stap niet gevonden in je plan.");
        }

        UpsertProgress(plan, userId, step.StepKey, step.Order, completed: false, CareerStepProgressSources.ManualUndo);
        plan.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await MaterializeAsync(plan, persistAuto: false, cancellationToken);
    }

    public async Task<HorizonCareerPathPlanView?> ClaimCourseAsync(
        Guid userId,
        string courseName,
        CancellationToken cancellationToken = default)
    {
        var name = (courseName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Geef een cursusnaam op.");
        }

        var plan = await RequirePlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new InvalidOperationException("Gebruiker niet gevonden.");
        var prefs = ParsePreferences(user.PreferencesJson);
        var certificates = (prefs.Certificates ?? []).ToList();
        if (!certificates.Any(c => string.Equals(c.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
        {
            certificates.Add(new CandidateCertificateDto(name, DateTime.UtcNow.Year));
            user.PreferencesJson = SerializePreferences(prefs with { Certificates = certificates });
            await _db.SaveChangesAsync(cancellationToken);
            _insightsQueue.TryEnqueue(userId);
        }

        return await MaterializeAsync(plan, persistAuto: true, cancellationToken);
    }

    private async Task<CandidateCareerPlan?> RequirePlanAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    private async Task<HorizonCareerPathPlanView> MaterializeAsync(
        CandidateCareerPlan plan,
        bool persistAuto,
        CancellationToken cancellationToken)
    {
        var stored = CareerPlanJson.Deserialize(plan.PlanJson);
        var certificates = await LoadCertificatesAsync(plan.UserId, cancellationToken);
        var signals = plan.StepProgress
            .Select(p => new CareerStepStatusResolver.ProgressSignal(p.StepKey, p.CompletedAtUtc, p.Source))
            .ToList();
        var inputs = stored
            .Select(s => new CareerStepStatusResolver.StepInput(s.StepKey, s.Order, s.Courses.ToList()))
            .ToList();
        var resolved = CareerStepStatusResolver.Resolve(inputs, signals, certificates);

        if (persistAuto)
        {
            var dirty = false;
            foreach (var step in resolved.Where(s => s.AutoCompletable))
            {
                UpsertProgress(plan, plan.UserId, step.StepKey, step.Order, completed: true, CareerStepProgressSources.Auto);
                dirty = true;
            }

            if (dirty)
            {
                plan.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                signals = plan.StepProgress
                    .Select(p => new CareerStepStatusResolver.ProgressSignal(p.StepKey, p.CompletedAtUtc, p.Source))
                    .ToList();
                resolved = CareerStepStatusResolver.Resolve(inputs, signals, certificates);
            }
        }

        var byKey = resolved.ToDictionary(s => s.StepKey, StringComparer.OrdinalIgnoreCase);
        var steps = stored.OrderBy(s => s.Order).Select(s =>
        {
            byKey.TryGetValue(s.StepKey, out var r);
            var status = r?.Status ?? HorizonCareerStepKind.Open;
            var matchPercent = r?.StepMatchPercent ?? 0;
            var matched = r?.MatchedCourseCount ?? 0;
            var courses = s.Courses
                .Select(c => new HorizonCareerCourseView(c, CareerCourseMatcher.IsOnProfile(c, certificates)))
                .ToList();
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
                s.ActionLabel,
                s.ActionHref,
                matchPercent,
                matched);
        }).ToList();

        return new HorizonCareerPathPlanView(
            plan.DreamTitle,
            plan.MatchPercent,
            plan.MatchSummary,
            CareerStepStatusResolver.GoalReached(resolved),
            steps);
    }

    private void UpsertProgress(
        CandidateCareerPlan plan,
        Guid userId,
        string stepKey,
        int order,
        bool completed,
        string source)
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

    private static string SerializePreferences(CandidatePreferencesDto prefs)
        => JsonSerializer.Serialize(prefs, PrefsJson);
}
