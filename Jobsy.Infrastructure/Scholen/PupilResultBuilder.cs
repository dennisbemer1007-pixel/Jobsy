using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>Scores the 60 pupil answers with the adult catalog math and writes <see cref="PupilResult"/>.</summary>
public sealed class PupilResultBuilder : IPupilResultBuilder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly JobsyDbContext _db;
    private readonly PupilQuestionBank _bank;
    private readonly TimeProvider _clock;

    public PupilResultBuilder(JobsyDbContext db, IPupilQuestionBank bank, TimeProvider? clock = null)
    {
        _db = db;
        _bank = bank as PupilQuestionBank ?? new PupilQuestionBank();
        _clock = clock ?? TimeProvider.System;
    }

    public async Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default)
    {
        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .FirstOrDefaultAsync(c => c.Id == pupilCodeId, cancellationToken);
        if (code?.Progress is null)
        {
            return;
        }

        var answersByString = ParseAnswers(code.Progress.AnswersJson);
        var answers = new Dictionary<int, int>();
        foreach (var (key, value) in answersByString)
        {
            if (int.TryParse(key, out var id) && LikertAnswerJson.IsValidAnswer(value))
            {
                answers[id] = value;
            }
        }

        var competence = CompetencyTestCatalog.Score(answers, _bank.AsCompetencyItems());
        var riasec = CareerTestCatalog.Score(answers, _bank.AsCareerItems());
        var values = SchwartzValuesCatalog.Score(answers, _bank.AsValuesItems());
        var culture = CulturePersonalityCatalog.Score(answers, _bank.AsCultureItems());

        var holland = CareerTestCatalog.HollandCode(riasec);
        var topValue = TopByCatalogOrder(values, SchwartzValuesCatalog.CategoryCodes, v => v switch
        {
            SchwartzValuesCatalog.Autonomy => values?.Autonomy,
            SchwartzValuesCatalog.Connection => values?.Connection,
            SchwartzValuesCatalog.Achievement => values?.Achievement,
            SchwartzValuesCatalog.Stability => values?.Stability,
            SchwartzValuesCatalog.Impact => values?.Impact,
            _ => null
        });
        var topCulture = TopByCatalogOrder(culture, CulturePersonalityCatalog.CultureDimensionCodes, c => c switch
        {
            CulturePersonalityCatalog.Autonomy => culture?.Autonomy,
            CulturePersonalityCatalog.Informal => culture?.Informal,
            CulturePersonalityCatalog.Collaboration => culture?.Collaboration,
            CulturePersonalityCatalog.Flexibility => culture?.Flexibility,
            CulturePersonalityCatalog.Innovation => culture?.Innovation,
            CulturePersonalityCatalog.PeopleFirst => culture?.PeopleFirst,
            _ => null
        });

        var now = _clock.GetUtcNow().UtcDateTime;
        var result = code.Result;
        if (result is null)
        {
            result = new PupilResult { PupilCodeId = code.Id };
            _db.PupilResults.Add(result);
            code.Result = result;
        }

        result.SchoolClassId = code.SchoolClassId;
        result.CompletedAtUtc = code.Progress.CompletedAtUtc ?? now;
        result.CompetenceScoresJson = JsonSerializer.Serialize(competence, Json);
        result.RiasecScoresJson = JsonSerializer.Serialize(riasec, Json);
        result.HollandCode = string.IsNullOrEmpty(holland) ? null : holland;
        result.ValuesScoresJson = JsonSerializer.Serialize(values, Json);
        result.TopValue = topValue;
        result.CultureScoresJson = JsonSerializer.Serialize(culture, Json);
        result.TopCulture = topCulture;
        result.ScoringVersion = PupilQuestionBank.ScoringVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.DreamJobKey = code.Progress.DreamJobKey;

        var likeKeys = ParseChipKeys(code.Progress.LikesJson);
        var storyKeys = PupilStoryTemplates.SelectKeys(result, likeKeys);
        result.StoryTemplateVersion = PupilStoryTemplates.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.StoryKeysJson = PupilStoryTemplates.Serialize(storyKeys);

        var fit = PupilDreamJobFit.Evaluate(result, code.Progress, code.Progress.DreamJobKey);
        result.FitSnapshotJson = fit is null ? null : PupilDreamJobFit.SerializeSnapshot(fit.Snapshot);

        code.Status = PupilCodeStatus.Completed;
        if (code.Progress.CompletedAtUtc is null)
        {
            code.Progress.CompletedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? TopByCatalogOrder<T>(
        T? scores,
        IReadOnlyList<string> order,
        Func<string, int?> getter)
    {
        if (scores is null)
        {
            return null;
        }

        string? best = null;
        var bestVal = int.MinValue;
        foreach (var code in order)
        {
            var v = getter(code);
            if (v is null)
            {
                continue;
            }

            if (v.Value > bestVal)
            {
                bestVal = v.Value;
                best = code;
            }
        }

        return best;
    }

    private static Dictionary<string, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }

    private static List<string> ParseChipKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
