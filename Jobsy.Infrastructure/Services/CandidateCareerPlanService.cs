using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateCareerPlanService : ICandidateCareerPlanService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly JobsyDbContext _db;
    private readonly ICareerPathPlanGenerationService _generator;

    public CandidateCareerPlanService(JobsyDbContext db, ICareerPathPlanGenerationService generator)
    {
        _db = db;
        _generator = generator;
    }

    public async Task<CareerPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await LoadPlanAsync(userId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        await EnsureAutoCompletionsAsync(plan, cancellationToken);
        return await BuildViewAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanView> GenerateAndSaveAsync(
        Guid userId,
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile,
        CancellationToken cancellationToken = default)
    {
        var dream = (dreamTitle ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dream))
        {
            throw new InvalidOperationException("Vul een stip op de horizon in.");
        }

        var generated = await _generator.GenerateAsync(dream, profile, cancellationToken);
        var now = DateTime.UtcNow;
        var content = ToContent(generated);
        var dreamKey = CareerStepKey.ForDream(generated.DreamTitle);

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
            DreamKey = dreamKey,
            PlanJson = SerializeSteps(content),
            MatchPercent = generated.MatchPercent,
            MatchSummary = generated.MatchSummary,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _db.CandidateCareerPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        await EnsureAutoCompletionsAsync(plan, cancellationToken);
        return await BuildViewAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanView> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await RequirePlanAsync(userId, cancellationToken);
        var steps = DeserializeSteps(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException("Stap niet gevonden.");

        await UpsertProgressAsync(
            plan,
            step,
            completedAtUtc: DateTime.UtcNow,
            source: CareerStepProgressSource.Manual,
            cancellationToken);

        return await BuildViewAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanView> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default)
    {
        var plan = await RequirePlanAsync(userId, cancellationToken);
        var steps = DeserializeSteps(plan.PlanJson);
        var step = steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException("Stap niet gevonden.");

        await UpsertProgressAsync(
            plan,
            step,
            completedAtUtc: null,
            source: CareerStepProgressSource.ManualUndo,
            cancellationToken);

        return await BuildViewAsync(plan, cancellationToken);
    }

    public async Task<CareerPlanView> MarkCourseOwnedAsync(
        Guid userId,
        string courseName,
        CancellationToken cancellationToken = default)
    {
        var name = (courseName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Cursusnaam ontbreekt.");
        }

        if (name.Length > 200)
        {
            name = name[..200];
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new InvalidOperationException("Gebruiker niet gevonden.");

        user.PreferencesJson = MergeCertificateIntoPreferences(user.PreferencesJson, name, DateTime.UtcNow.Year);
        await _db.SaveChangesAsync(cancellationToken);

        var plan = await RequirePlanAsync(userId, cancellationToken);
        await EnsureAutoCompletionsAsync(plan, cancellationToken);
        return await BuildViewAsync(plan, cancellationToken);
    }

    internal static string MergeCertificateIntoPreferences(string? preferencesJson, string courseName, int year)
    {
        JsonObject root;
        try
        {
            root = string.IsNullOrWhiteSpace(preferencesJson)
                ? new JsonObject()
                : JsonNode.Parse(preferencesJson) as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            root = new JsonObject();
        }

        var certificates = root["certificates"] as JsonArray ?? new JsonArray();
        foreach (var item in certificates)
        {
            var existingName = item?["name"]?.GetValue<string>()?.Trim();
            if (string.Equals(existingName, courseName, StringComparison.OrdinalIgnoreCase))
            {
                root["certificates"] = certificates;
                return root.ToJsonString(JsonOptions);
            }
        }

        certificates.Add(new JsonObject
        {
            ["name"] = courseName,
            ["year"] = year
        });
        root["certificates"] = certificates;
        return root.ToJsonString(JsonOptions);
    }

    private async Task EnsureAutoCompletionsAsync(CandidateCareerPlan plan, CancellationToken cancellationToken)
    {
        var steps = DeserializeSteps(plan.PlanJson);
        var progress = MapProgress(plan.StepProgress);
        var certs = await LoadCertificateNamesAsync(plan.UserId, cancellationToken);
        var needing = CareerStepStatusResolver.StepsNeedingAutoComplete(steps, progress, certs);
        if (needing.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var step in needing)
        {
            await UpsertProgressAsync(plan, step, now, CareerStepProgressSource.Auto, cancellationToken);
        }
    }

    private async Task UpsertProgressAsync(
        CandidateCareerPlan plan,
        CareerPlanStepContent step,
        DateTime? completedAtUtc,
        CareerStepProgressSource source,
        CancellationToken cancellationToken)
    {
        var row = plan.StepProgress.FirstOrDefault(p =>
            string.Equals(p.StepKey, step.StepKey, StringComparison.OrdinalIgnoreCase));
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateCareerStepProgress
            {
                Id = Guid.NewGuid(),
                UserId = plan.UserId,
                PlanId = plan.Id,
                StepKey = step.StepKey,
                StepOrder = step.Order
            };
            plan.StepProgress.Add(row);
            _db.CandidateCareerStepProgress.Add(row);
        }

        row.StepOrder = step.Order;
        row.CompletedAtUtc = completedAtUtc;
        row.Source = source.ToString();
        row.UpdatedAtUtc = now;
        plan.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CareerPlanView> BuildViewAsync(CandidateCareerPlan plan, CancellationToken cancellationToken)
    {
        // Reload progress after mutations
        await _db.Entry(plan).Collection(p => p.StepProgress).LoadAsync(cancellationToken);
        var steps = DeserializeSteps(plan.PlanJson);
        var progress = MapProgress(plan.StepProgress);
        var certs = await LoadCertificateNamesAsync(plan.UserId, cancellationToken);
        return CareerStepStatusResolver.Resolve(
            plan.Id,
            plan.DreamTitle,
            plan.DreamKey,
            plan.MatchPercent,
            plan.MatchSummary,
            steps,
            progress,
            certs);
    }

    private async Task<CandidateCareerPlan?> LoadPlanAsync(Guid userId, CancellationToken cancellationToken)
        => await _db.CandidateCareerPlans
            .Include(p => p.StepProgress)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    private async Task<CandidateCareerPlan> RequirePlanAsync(Guid userId, CancellationToken cancellationToken)
        => await LoadPlanAsync(userId, cancellationToken)
           ?? throw new InvalidOperationException("Nog geen stappenplan. Kies eerst een stip op de horizon.");

    private async Task<IReadOnlyList<string?>> LoadCertificateNamesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var json = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync(cancellationToken);
        var prefs = ParsePreferences(json);
        return (prefs.Certificates ?? Array.Empty<CandidateCertificateDto>())
            .Select(c => c.Name)
            .ToList();
    }

    private static IReadOnlyList<CareerStepProgressSnapshot> MapProgress(IEnumerable<CandidateCareerStepProgress> rows)
        => rows.Select(r => new CareerStepProgressSnapshot(
            r.StepKey,
            r.StepOrder,
            r.CompletedAtUtc,
            Enum.TryParse<CareerStepProgressSource>(r.Source, true, out var src)
                ? src
                : CareerStepProgressSource.Manual,
            r.UpdatedAtUtc)).ToList();

    public static IReadOnlyList<CareerPlanStepContent> ToContent(HorizonCareerPathPlan plan)
        => plan.Steps.Select(s => new CareerPlanStepContent(
            string.IsNullOrWhiteSpace(s.Id) ? CareerStepKey.ForStep(s.Title, s.Order) : s.Id,
            s.Order,
            s.Title,
            s.Summary,
            s.SkillsGap.ToList(),
            s.Courses.ToList(),
            s.MinRequirements.ToList(),
            s.YearsExperienceNeeded,
            s.ActionLabel,
            s.ActionHref)).ToList();

    public static string SerializeSteps(IReadOnlyList<CareerPlanStepContent> steps)
        => JsonSerializer.Serialize(
            new PlanJsonEnvelope
            {
                Steps = steps.Select(s => new PlanStepJson
                {
                    StepKey = s.StepKey,
                    Order = s.Order,
                    Title = s.Title,
                    Summary = s.Summary,
                    SkillsGap = s.SkillsGap.ToList(),
                    Courses = s.Courses.ToList(),
                    MinRequirements = s.MinRequirements.ToList(),
                    YearsExperienceNeeded = s.YearsExperienceNeeded,
                    ActionLabel = s.ActionLabel,
                    ActionHref = s.ActionHref
                }).ToList()
            },
            JsonOptions);

    public static IReadOnlyList<CareerPlanStepContent> DeserializeSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<PlanJsonEnvelope>(json, JsonOptions);
            return (envelope?.Steps ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s.StepKey))
                .OrderBy(s => s.Order)
                .Select(s => new CareerPlanStepContent(
                    s.StepKey,
                    s.Order,
                    s.Title ?? "",
                    s.Summary ?? "",
                    s.SkillsGap ?? [],
                    s.Courses ?? [],
                    s.MinRequirements ?? [],
                    s.YearsExperienceNeeded,
                    s.ActionLabel ?? "",
                    s.ActionHref ?? ""))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static CandidatePreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, JsonOptions)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }

    private sealed class PlanJsonEnvelope
    {
        public List<PlanStepJson> Steps { get; set; } = [];
    }

    private sealed class PlanStepJson
    {
        public string StepKey { get; set; } = "";
        public int Order { get; set; }
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public List<string>? SkillsGap { get; set; }
        public List<string>? Courses { get; set; }
        public List<string>? MinRequirements { get; set; }
        public int YearsExperienceNeeded { get; set; }
        public string? ActionLabel { get; set; }
        public string? ActionHref { get; set; }
    }
}
