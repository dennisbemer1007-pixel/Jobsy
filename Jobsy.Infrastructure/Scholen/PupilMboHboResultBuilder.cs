using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public interface IPupilMboHboResultBuilder
{
    Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default);
}

public sealed class PupilMboHboResultBuilder : IPupilMboHboResultBuilder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly JobsyDbContext _db;
    private readonly TimeProvider _clock;

    public PupilMboHboResultBuilder(JobsyDbContext db, TimeProvider? clock = null)
    {
        _db = db;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default)
    {
        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .Include(c => c.SchoolClass)
            .FirstOrDefaultAsync(c => c.Id == pupilCodeId, cancellationToken);
        if (code?.Progress is null || code.SchoolClass?.QuestionSet != PupilQuestionSet.MboHbo)
        {
            return;
        }

        var state = PupilMboHboProgressState.Parse(code.Progress.MboHboProgressJson);
        if (!state.AllBlocksComplete())
        {
            return;
        }

        var level = code.SchoolClass.Level;
        var interesse = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Interesse, state, level);
        var riasecJson = JsonSerializer.Serialize(
            interesse.Bars.ToDictionary(b => RiasecKey(b.Label), b => b.Percent),
            Json);

        var competence = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Competentie, state, level);
        var competenceJson = JsonSerializer.Serialize(
            competence.Bars.ToDictionary(b => b.Label, b => b.Percent),
            Json);

        var values = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Werkwaarden, state, level);
        var valuesJson = JsonSerializer.Serialize(
            values.Bars
                .GroupBy(b => b.Label, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Max(x => x.Percent), StringComparer.OrdinalIgnoreCase),
            Json);

        var holland = BuildHolland(interesse.Bars);
        var topValue = values.Highlights.Count > 0
            ? values.Highlights[0]
            : values.Bars.Count > 0 ? values.Bars[0].Label : null;

        var now = _clock.GetUtcNow().UtcDateTime;
        var result = code.Result ?? new PupilResult { PupilCodeId = code.Id };
        if (code.Result is null)
        {
            _db.PupilResults.Add(result);
            code.Result = result;
        }

        result.SchoolClassId = code.SchoolClassId;
        result.CompletedAtUtc = now;
        result.ScoringVersion = PupilMboHboCatalog.ScoringVersion;
        result.StoryTemplateVersion = "mbo-hbo-1";
        result.RiasecScoresJson = riasecJson;
        result.CompetenceScoresJson = competenceJson;
        result.ValuesScoresJson = valuesJson;
        result.HollandCode = holland;
        result.TopValue = topValue;
        result.CultureScoresJson = "{}";
        result.StoryKeysJson = "[]";

        code.Status = PupilCodeStatus.Completed;
        code.Progress.CompletedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildHolland(IReadOnlyList<PupilMboHboScoring.NamedScore> bars)
    {
        var map = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
        {
            ["R (Praktisch)"] = 'R',
            ["Praktisch"] = 'R',
            ["I (Onderzoekend)"] = 'I',
            ["Onderzoekend"] = 'I',
            ["A (Artistiek)"] = 'A',
            ["Artistiek"] = 'A',
            ["S (Sociaal)"] = 'S',
            ["Sociaal"] = 'S',
            ["E (Ondernemend)"] = 'E',
            ["Ondernemend"] = 'E',
            ["C (Conventioneel)"] = 'C',
            ["Conventioneel"] = 'C'
        };

        return string.Concat(bars
            .OrderByDescending(b => b.Percent)
            .Select(b => map.TryGetValue(b.Label, out var c) ? c : (char?)null)
            .Where(c => c is not null)
            .Take(3)
            .Select(c => c!.Value));
    }

    private static string RiasecKey(string label)
        => label switch
        {
            "R (Praktisch)" or "Praktisch" => CareerTestCatalog.Realistic,
            "I (Onderzoekend)" or "Onderzoekend" => CareerTestCatalog.Investigative,
            "A (Artistiek)" or "Artistiek" => CareerTestCatalog.Artistic,
            "S (Sociaal)" or "Sociaal" => CareerTestCatalog.Social,
            "E (Ondernemend)" or "Ondernemend" => CareerTestCatalog.Enterprising,
            "C (Conventioneel)" or "Conventioneel" => CareerTestCatalog.Conventional,
            _ => label
        };
}
