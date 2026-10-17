using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateDiscoveryOverviewService(JobsyDbContext db) : ICandidateDiscoveryOverviewService
{
    public async Task<CandidateDiscoveryOverviewPanelDto> GetPanelAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var competency = await db.CandidateCompetencies.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.UpdatedAtUtc, c.AnswersJson })
            .FirstOrDefaultAsync(cancellationToken);
        var career = await db.CandidateCareerInterests.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.UpdatedAtUtc, c.AnswersJson })
            .FirstOrDefaultAsync(cancellationToken);
        var culture = await db.CandidateCulturePersonalityProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.UpdatedAtUtc, c.AnswersJson })
            .FirstOrDefaultAsync(cancellationToken);
        var values = await db.CandidateValuesProfiles.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.UpdatedAtUtc, c.AnswersJson })
            .FirstOrDefaultAsync(cancellationToken);
        var onboarding = await db.CandidateOnboardings.AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.CompletedAtUtc, o.StartedAtUtc, o.StepsJson })
            .FirstOrDefaultAsync(cancellationToken);

        var revision = $"{BuildRevision(
            onboarding?.CompletedAtUtc,
            onboarding?.StartedAtUtc,
            competency?.UpdatedAtUtc,
            career?.UpdatedAtUtc,
            culture?.UpdatedAtUtc,
            values?.UpdatedAtUtc)}:{onboarding?.StepsJson?.Length ?? 0}";

        return new CandidateDiscoveryOverviewPanelDto(
            revision,
            CountValidAnswers(competency?.AnswersJson),
            CountValidAnswers(career?.AnswersJson),
            CountValidAnswers(culture?.AnswersJson),
            CountValidAnswers(values?.AnswersJson));
    }

    internal static string BuildRevision(params DateTime?[] stamps)
    {
        var sb = new StringBuilder(64);
        foreach (var stamp in stamps)
        {
            sb.Append(stamp?.Ticks.ToString("x") ?? "0").Append('|');
        }

        return sb.ToString();
    }

    internal static int CountValidAnswers(string? answersJson)
    {
        if (string.IsNullOrWhiteSpace(answersJson) || answersJson == "{}")
        {
            return 0;
        }

        try
        {
            using var doc = JsonDocument.Parse(answersJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }

            var count = 0;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number
                    && prop.Value.TryGetInt32(out var n)
                    && n is >= 1 and <= 5)
                {
                    count++;
                }
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }
}
