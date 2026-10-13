using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// One-off: stored stories that invent facts are replaced by the fact-only template.
/// The next passport view can ask the model again after the cooldown.
/// </summary>
public sealed class WhoAmIStoryBackfillHostedService : BackgroundService
{
    public const string MarkerCategory = "whoami.story.backfill";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhoAmIStoryBackfillHostedService> _logger;

    public WhoAmIStoryBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WhoAmIStoryBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var replaced = await RunOnceAsync(db, stoppingToken);
            if (replaced >= 0)
            {
                _logger.LogInformation("WhoAmI story backfill replaced {Count} stories.", replaced);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "WhoAmI story backfill failed.");
        }
    }

    public static async Task<int> RunOnceAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var already = await db.PlatformLogs.AsNoTracking()
            .AnyAsync(l => l.Category == MarkerCategory, cancellationToken);
        if (already)
        {
            return -1;
        }

        var rows = await db.CandidateWhoAmIProfiles.ToListAsync(cancellationToken);
        var replaced = 0;
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var story = WhoAmIStoryBuilder.Sanitize(row.StoryText);
            if (story is null)
            {
                continue;
            }

            var competency = await CompletedCompetencyAsync(db, row.UserId, cancellationToken);
            var career = await CompletedCareerAsync(db, row.UserId, cancellationToken);
            var culture = await CompletedCultureAsync(db, row.UserId, cancellationToken);
            if (competency is null || career is null || culture is null)
            {
                continue;
            }

            var highlights = WhoAmIProfileHighlights.Empty;
            var sheet = CandidateFactSheet.ForWhoAmI(competency, career, culture, highlights, values: null);
            var ok = CandidateFactGuard.RejectionReason(story, sheet) is null
                     && WhoAmIStoryBuilder.Accepts(story, highlights, competency, culture, career);
            if (ok)
            {
                continue;
            }

            row.StoryText = WhoAmIStoryBuilder.Build(competency, career, culture, highlights, employersEnabled: false);
            row.FromOpenAi = false;
            row.InputFingerprint = "";
            row.LastAttemptUtc = null;
            row.UpdatedAtUtc = now;
            replaced++;
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = MarkerCategory,
            Message = "WhoAmI story fact backfill completed",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return replaced;
    }

    private static async Task<CompetencyScores?> CompletedCompetencyAsync(
        JobsyDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var scores = new CompetencyScores(
            row.SamenwerkenPercent,
            row.ResultaatgerichtheidPercent,
            row.StressbestendigheidPercent,
            row.InnovatiePercent,
            row.ExtraversiePercent);
        return scores.IsComplete ? scores : null;
    }

    private static async Task<RiasecScores?> CompletedCareerAsync(
        JobsyDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        return CareerTestCatalog.CompletedScoresOrNull(
            row?.Status,
            row?.RealisticPercent,
            row?.InvestigativePercent,
            row?.ArtisticPercent,
            row?.SocialPercent,
            row?.EnterprisingPercent,
            row?.ConventionalPercent);
    }

    private static async Task<CulturePersonalityScores?> CompletedCultureAsync(
        JobsyDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return null;
        }

        var scores = new CulturePersonalityScores(
            row.AutonomyPercent,
            row.InformalPercent,
            row.CollaborationPercent,
            row.FlexibilityPercent,
            row.InnovationPercent,
            row.PeopleFirstPercent,
            row.OpennessPercent,
            row.ConscientiousnessPercent,
            row.ExtraversionPercent,
            row.AgreeablenessPercent,
            row.EmotionalStabilityPercent);
        return scores.IsComplete ? scores : null;
    }
}
