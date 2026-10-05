using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Entities;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// One-shot fill of missing ESCO days. Existing rows are skipped. Each save is its own commit, so a run can resume.
/// </summary>
public sealed class OccupationDayInLifeGenerator
{
    public const int MaxImportRows = 8000;

    private readonly JobsyDbContext _db;
    private readonly IOccupationDayInLifeWriter _writer;
    private readonly OccupationDayInLifeOptions _options;
    private readonly ILogger<OccupationDayInLifeGenerator> _logger;

    public OccupationDayInLifeGenerator(
        JobsyDbContext db,
        IOccupationDayInLifeWriter writer,
        IOptions<OccupationDayInLifeOptions> options,
        ILogger<OccupationDayInLifeGenerator> logger)
    {
        _db = db;
        _writer = writer;
        _options = options.Value;
        _logger = logger;
    }

    public Task<OccupationDayGenerateResult> GenerateMissingAsync(int limit, CancellationToken cancellationToken = default)
        => GenerateMissingAsync(limit, skipEscoIds: null, onlyEscoIds: null, cancellationToken);

    public async Task<OccupationDayGenerateResult> GenerateMissingAsync(
        int limit,
        IReadOnlySet<string>? skipEscoIds,
        IReadOnlySet<string>? onlyEscoIds,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var have = await _db.OccupationDayInLives.AsNoTracking()
            .Select(item => item.EscoId)
            .ToListAsync(cancellationToken);
        var done = have.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var queue = OccupationCatalog.Shared.All
            .Where(job => onlyEscoIds is null || onlyEscoIds.Contains(job.Id))
            .Where(job => !done.Contains(job.Id))
            .Where(job => skipEscoIds is null || !skipEscoIds.Contains(job.Id))
            .Take(limit)
            .ToList();
        var remaining = OccupationCatalog.Shared.All.Count(job =>
            (onlyEscoIds is null || onlyEscoIds.Contains(job.Id)) && !done.Contains(job.Id));
        if (queue.Count == 0)
        {
            return new OccupationDayGenerateResult(0, 0, done.Count, remaining, false, []);
        }

        var generated = 0;
        var skipped = 0;
        var failures = new List<OccupationDayFailure>();
        for (var i = 0; i < queue.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (i > 0 && _options.DelayMilliseconds > 0)
            {
                await Task.Delay(_options.DelayMilliseconds, cancellationToken);
            }

            var facts = OccupationDayFacts.For(queue[i].Id);
            if (facts is null)
            {
                failures.Add(new OccupationDayFailure(queue[i].Id, "onbekend-beroep"));
                continue;
            }

            if (await _db.OccupationDayInLives.AnyAsync(item => item.EscoId == facts.EscoId, cancellationToken))
            {
                skipped++;
                continue;
            }

            var (draft, error, keyMissing, model) = await AskAsync(facts, facts.ToPrompt(), cancellationToken);
            if (keyMissing)
            {
                return new OccupationDayGenerateResult(
                    generated,
                    failures.Count,
                    skipped,
                    Math.Max(0, remaining - generated),
                    true,
                    failures);
            }

            if (draft is null)
            {
                var retryUser = facts.ToPrompt() + "\n\nAfgekeurd: " + error + "\n" + OccupationDayInLifePrompt.Retry;
                if (_options.DelayMilliseconds > 0)
                {
                    await Task.Delay(_options.DelayMilliseconds, cancellationToken);
                }

                (draft, error, keyMissing, model) = await AskAsync(facts, retryUser, cancellationToken);
                if (keyMissing)
                {
                    return new OccupationDayGenerateResult(
                        generated,
                        failures.Count,
                        skipped,
                        Math.Max(0, remaining - generated),
                        true,
                        failures);
                }
            }

            if (draft is null)
            {
                _logger.LogInformation(
                    "Dag-in-het-leven afgekeurd voor beroep {EscoId}: {Reason}",
                    facts.EscoId,
                    error);
                failures.Add(new OccupationDayFailure(facts.EscoId, error ?? "afgekeurd"));
                continue;
            }

            var entity = ToEntity(facts, draft, model);
            _db.OccupationDayInLives.Add(entity);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                generated++;
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                skipped++;
            }
        }

        var left = Math.Max(0, remaining - generated - skipped);
        return new OccupationDayGenerateResult(generated, failures.Count, done.Count + skipped, left, false, failures);
    }

    public async Task<OccupationDayExportDocument> ExportAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.OccupationDayInLives.AsNoTracking()
            .OrderBy(item => item.Uri)
            .ToListAsync(cancellationToken);
        return new OccupationDayExportDocument
        {
            Locale = "nl",
            ExportedAtUtc = DateTime.UtcNow,
            Rows = rows.Select(ToExportRow).ToList()
        };
    }

    public async Task<OccupationDayImportResult> ImportAsync(
        OccupationDayExportDocument? document,
        CancellationToken cancellationToken = default)
    {
        if (document?.Rows is null || document.Rows.Count == 0)
        {
            return new OccupationDayImportResult(0, 0, 0, 0);
        }

        if (document.Rows.Count > MaxImportRows)
        {
            return new OccupationDayImportResult(0, 0, 0, document.Rows.Count);
        }

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var rejected = 0;
        foreach (var row in document.Rows)
        {
            if (!TryAccept(row, out var facts, out var draft, out var hash))
            {
                rejected++;
                continue;
            }

            var existing = await _db.OccupationDayInLives
                .FirstOrDefaultAsync(item => item.EscoId == facts!.EscoId, cancellationToken);
            if (existing is not null && string.Equals(existing.ContentHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                unchanged++;
                continue;
            }

            if (existing is null)
            {
                _db.OccupationDayInLives.Add(ToEntity(facts!, draft!, row.SourceModel, row.GeneratedAtUtc, hash));
                inserted++;
            }
            else
            {
                Apply(existing, facts!, draft!, row.SourceModel, row.GeneratedAtUtc, hash);
                updated++;
            }
        }

        if (inserted + updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new OccupationDayImportResult(inserted, updated, unchanged, rejected);
    }

    private async Task<(OccupationDayDraft? Draft, string? Error, bool KeyMissing, string Model)> AskAsync(
        OccupationDayFacts facts,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var write = await _writer.CompleteAsync(OccupationDayInLifePrompt.System, userPrompt, cancellationToken);
        var model = string.IsNullOrWhiteSpace(write.Model) ? _options.Model : write.Model;
        if (write.KeyMissing)
        {
            return (null, write.Error, true, model);
        }

        if (!write.Ok)
        {
            return (null, write.Error ?? OccupationDayWriteErrors.Http, false, model);
        }

        if (!OccupationDayInLifeJson.TryParse(write.Json, facts, out var draft, out var error))
        {
            return (null, error ?? "onleesbaar", false, model);
        }

        if (!OccupationDayInLifeValidator.TryValidate(draft, facts, out var reasons))
        {
            return (null, string.Join("; ", reasons), false, model);
        }

        return (draft, null, false, model);
    }

    private static bool TryAccept(
        OccupationDayExportRow row,
        out OccupationDayFacts? facts,
        out OccupationDayDraft? draft,
        out string hash)
    {
        facts = null;
        draft = null;
        hash = "";
        if (row is null
            || !string.Equals(row.Locale, "nl", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        facts = OccupationDayFacts.For(row.EscoId);
        if (facts is null)
        {
            return false;
        }

        draft = new OccupationDayDraft(
            facts.TitleNl,
            (row.Morning ?? "").Trim(),
            (row.Midday ?? "").Trim(),
            (row.Afternoon ?? "").Trim(),
            (row.Highlights ?? []).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(4).ToList(),
            (row.VariesNote ?? "").Trim());
        if (!OccupationDayInLifeValidator.TryValidate(draft, facts, out _))
        {
            return false;
        }

        hash = OccupationDayInLifeHash.Compute(facts.EscoId, draft);
        return string.Equals(hash, (row.ContentHash ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static OccupationDayInLife ToEntity(OccupationDayFacts facts, OccupationDayDraft draft, string model)
        => ToEntity(facts, draft, model, DateTime.UtcNow, OccupationDayInLifeHash.Compute(facts.EscoId, draft));

    private static OccupationDayInLife ToEntity(
        OccupationDayFacts facts,
        OccupationDayDraft draft,
        string? model,
        DateTime generatedAtUtc,
        string hash)
        => new()
        {
            Id = Guid.NewGuid(),
            EscoId = facts.EscoId,
            Uri = Trim(facts.Uri, 120),
            TitleNl = Trim(facts.TitleNl, 160),
            Morning = draft.Morning,
            Midday = draft.Midday,
            Afternoon = draft.Afternoon,
            HighlightsJson = JsonSerializer.Serialize(draft.Highlights),
            VariesNote = draft.VariesNote,
            SourceModel = Trim(string.IsNullOrWhiteSpace(model) ? OccupationDayInLifeOptions.DefaultModel : model, 80),
            GeneratedAtUtc = generatedAtUtc.Kind == DateTimeKind.Utc
                ? generatedAtUtc
                : DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc),
            ContentHash = hash,
            Locale = "nl",
            ThinSource = facts.IsThin
        };

    private static void Apply(
        OccupationDayInLife existing,
        OccupationDayFacts facts,
        OccupationDayDraft draft,
        string? model,
        DateTime generatedAtUtc,
        string hash)
    {
        existing.Uri = Trim(facts.Uri, 120);
        existing.TitleNl = Trim(facts.TitleNl, 160);
        existing.Morning = draft.Morning;
        existing.Midday = draft.Midday;
        existing.Afternoon = draft.Afternoon;
        existing.HighlightsJson = JsonSerializer.Serialize(draft.Highlights);
        existing.VariesNote = draft.VariesNote;
        existing.SourceModel = Trim(string.IsNullOrWhiteSpace(model) ? existing.SourceModel : model, 80);
        existing.GeneratedAtUtc = generatedAtUtc.Kind == DateTimeKind.Utc
            ? generatedAtUtc
            : DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc);
        existing.ContentHash = hash;
        existing.Locale = "nl";
        existing.ThinSource = facts.IsThin;
    }

    private static OccupationDayExportRow ToExportRow(OccupationDayInLife row)
    {
        List<string> highlights;
        try
        {
            highlights = JsonSerializer.Deserialize<List<string>>(row.HighlightsJson) ?? [];
        }
        catch (JsonException)
        {
            highlights = [];
        }

        return new OccupationDayExportRow
        {
            EscoId = row.EscoId,
            Uri = row.Uri,
            TitleNl = row.TitleNl,
            Morning = row.Morning,
            Midday = row.Midday,
            Afternoon = row.Afternoon,
            Highlights = highlights,
            VariesNote = row.VariesNote,
            SourceModel = row.SourceModel,
            GeneratedAtUtc = row.GeneratedAtUtc,
            ContentHash = row.ContentHash,
            Locale = row.Locale,
            ThinSource = row.ThinSource
        };
    }

    private static string Trim(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
