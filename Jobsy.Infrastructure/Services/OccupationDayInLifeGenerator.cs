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
/// One-shot fill of missing ESCO days and their stored translations.
/// A Dutch row is never rewritten. A language is translated once and then only read.
/// </summary>
public sealed class OccupationDayInLifeGenerator
{
    public const int MaxImportRows = 8000;

    private readonly JobsyDbContext _db;
    private readonly IOccupationDayInLifeWriter _writer;
    private readonly IOccupationDayInLifeTranslator _translator;
    private readonly OccupationDayInLifeOptions _options;
    private readonly ILogger<OccupationDayInLifeGenerator> _logger;

    public OccupationDayInLifeGenerator(
        JobsyDbContext db,
        IOccupationDayInLifeWriter writer,
        IOccupationDayInLifeTranslator translator,
        IOptions<OccupationDayInLifeOptions> options,
        ILogger<OccupationDayInLifeGenerator> logger)
    {
        _db = db;
        _writer = writer;
        _translator = translator;
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
        var stored = await _db.OccupationDayInLives.AsNoTracking()
            .Select(item => new { item.EscoId, item.ContentHash, item.TranslationsJson })
            .ToListAsync(cancellationToken);
        var complete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in stored)
        {
            if (OccupationDayTranslations.IsComplete(row.TranslationsJson, row.ContentHash))
            {
                complete.Add(row.EscoId);
            }
        }

        var queue = OccupationCatalog.Shared.All
            .Where(job => onlyEscoIds is null || onlyEscoIds.Contains(job.Id))
            .Where(job => !complete.Contains(job.Id))
            .Where(job => skipEscoIds is null || !skipEscoIds.Contains(job.Id))
            .Take(limit)
            .ToList();
        var remaining = OccupationCatalog.Shared.All.Count(job =>
            (onlyEscoIds is null || onlyEscoIds.Contains(job.Id)) && !complete.Contains(job.Id));
        if (queue.Count == 0)
        {
            return new OccupationDayGenerateResult(0, 0, complete.Count, remaining, false, []);
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

            var existing = await _db.OccupationDayInLives
                .FirstOrDefaultAsync(item => item.EscoId == facts.EscoId, cancellationToken);
            if (existing is not null)
            {
                if (OccupationDayTranslations.IsComplete(existing.TranslationsJson, existing.ContentHash))
                {
                    skipped++;
                    continue;
                }

                var filled = await FillTranslationsAsync(existing, DraftFrom(existing), failures, cancellationToken);
                if (filled == FillOutcome.KeyMissing)
                {
                    return new OccupationDayGenerateResult(
                        generated,
                        failures.Count,
                        skipped,
                        Math.Max(0, remaining - generated),
                        true,
                        failures);
                }

                if (filled == FillOutcome.Complete)
                {
                    generated++;
                }

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
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                skipped++;
                continue;
            }

            var afterDutch = await FillTranslationsAsync(entity, draft, failures, cancellationToken);
            if (afterDutch == FillOutcome.KeyMissing)
            {
                return new OccupationDayGenerateResult(
                    generated + 1,
                    failures.Count,
                    skipped,
                    Math.Max(0, remaining - generated - 1),
                    true,
                    failures);
            }

            generated++;
        }

        var left = Math.Max(0, remaining - generated - skipped);
        return new OccupationDayGenerateResult(generated, failures.Count, complete.Count + skipped, left, false, failures);
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
                if (MergeImportedTranslations(existing, row, hash))
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            if (existing is null)
            {
                var created = ToEntity(facts!, draft!, row.SourceModel, row.GeneratedAtUtc, hash);
                created.TranslationsJson = AcceptedTranslationsJson(draft!, row, hash);
                _db.OccupationDayInLives.Add(created);
                inserted++;
            }
            else
            {
                Apply(existing, facts!, draft!, row.SourceModel, row.GeneratedAtUtc, hash);
                existing.TranslationsJson = AcceptedTranslationsJson(draft!, row, hash);
                updated++;
            }
        }

        if (inserted + updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new OccupationDayImportResult(inserted, updated, unchanged, rejected);
    }

    private enum FillOutcome
    {
        Complete,
        Partial,
        KeyMissing
    }

    private async Task<FillOutcome> FillTranslationsAsync(
        OccupationDayInLife row,
        OccupationDayDraft dutch,
        List<OccupationDayFailure> failures,
        CancellationToken cancellationToken)
    {
        var map = OccupationDayTranslations.Parse(row.TranslationsJson);
        var changed = false;
        var missing = false;
        foreach (var language in OccupationDayTranslations.TargetLanguages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (OccupationDayTranslations.TryGet(OccupationDayTranslations.Serialize(map), language, row.ContentHash, out _))
            {
                continue;
            }

            if (changed && _options.DelayMilliseconds > 0)
            {
                await Task.Delay(_options.DelayMilliseconds, cancellationToken);
            }

            var result = await TranslateOnceAsync(dutch, language, cancellationToken);
            if (result.KeyMissing)
            {
                if (changed)
                {
                    row.TranslationsJson = OccupationDayTranslations.Serialize(map);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                return FillOutcome.KeyMissing;
            }

            IReadOnlyList<string> reasons = [];
            var valid = result.Draft is not null
                && OccupationDayInLifeValidator.TryValidateTranslation(dutch, result.Draft, out reasons);
            if (!valid || result.Draft is null)
            {
                missing = true;
                var detail = reasons.Count == 0 ? "" : ":" + string.Join(';', reasons);
                failures.Add(new OccupationDayFailure(row.EscoId, "vertaling-" + language + detail));
                continue;
            }

            map[language] = OccupationDayTranslations.FromDraft(result.Draft, row.ContentHash, result.Model);
            changed = true;
        }

        if (changed)
        {
            row.TranslationsJson = OccupationDayTranslations.Serialize(map);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return missing || !OccupationDayTranslations.IsComplete(row.TranslationsJson, row.ContentHash)
            ? FillOutcome.Partial
            : FillOutcome.Complete;
    }

    private async Task<OccupationDayTranslateResult> TranslateOnceAsync(
        OccupationDayDraft dutch,
        string language,
        CancellationToken cancellationToken)
    {
        var result = await _translator.TranslateAsync(dutch, language, cancellationToken);
        if (result.KeyMissing || (result.Ok && result.Draft is not null && OccupationDayInLifeValidator.TryValidateTranslation(dutch, result.Draft, out _)))
        {
            return result;
        }

        if (_options.DelayMilliseconds > 0)
        {
            await Task.Delay(_options.DelayMilliseconds, cancellationToken);
        }

        return await _translator.TranslateAsync(dutch, language, cancellationToken);
    }

    private static OccupationDayDraft DraftFrom(OccupationDayInLife row)
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

        return new OccupationDayDraft(
            row.TitleNl,
            row.Morning,
            row.Midday,
            row.Afternoon,
            row.Closing,
            highlights,
            row.VariesNote,
            OccupationDayBlocks.Parse(row.BlocksJson),
            ReadLines(row.TasksJson),
            ReadLines(row.SkillsJson));
    }

    private static OccupationDayDraft AttachFacts(OccupationDayDraft draft, OccupationDayFacts facts, bool forceCatalog)
    {
        var tasks = !forceCatalog && draft.Tasks is { Count: > 0 } ? draft.Tasks : facts.Tasks;
        var skills = !forceCatalog && draft.Skills is { Count: > 0 } ? draft.Skills : facts.Skills;
        return OccupationDayBlocks.WithDerived(draft) with
        {
            Tasks = tasks.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(OccupationDayFacts.MaxTasks).ToList(),
            Skills = skills.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(8).ToList()
        };
    }

    private static List<string> ReadLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string AcceptedTranslationsJson(OccupationDayDraft dutch, OccupationDayExportRow row, string hash)
    {
        var incoming = row.Translations;
        if (incoming is null || incoming.Count == 0)
        {
            return "{}";
        }

        var map = new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in incoming)
        {
            if (pair.Value is null)
            {
                continue;
            }

            var stored = pair.Value;
            stored.SourceHash = string.IsNullOrWhiteSpace(stored.SourceHash) ? hash : stored.SourceHash;
            if (!string.Equals(stored.SourceHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var draft = OccupationDayTranslations.ToDraft(stored, dutch.TitleNl);
            if (!OccupationDayInLifeValidator.TryValidateTranslation(dutch, draft, out _))
            {
                continue;
            }

            map[pair.Key] = OccupationDayTranslations.FromDraft(draft, hash, stored.Model);
        }

        return map.Count == 0 ? "{}" : OccupationDayTranslations.Serialize(map);
    }

    private static bool MergeImportedTranslations(OccupationDayInLife existing, OccupationDayExportRow row, string hash)
    {
        var incoming = row.Translations ?? new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase);
        if (incoming.Count == 0)
        {
            return false;
        }

        var map = OccupationDayTranslations.Parse(existing.TranslationsJson);
        var changed = false;
        foreach (var pair in incoming)
        {
            var stored = pair.Value;
            if (stored is null)
            {
                continue;
            }

            stored.SourceHash = string.IsNullOrWhiteSpace(stored.SourceHash) ? hash : stored.SourceHash;
            if (!string.Equals(stored.SourceHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var draft = OccupationDayTranslations.ToDraft(stored, existing.TitleNl);
            var dutch = DraftFrom(existing);
            if (!OccupationDayInLifeValidator.TryValidateTranslation(dutch, draft, out _))
            {
                continue;
            }

            map[pair.Key] = OccupationDayTranslations.FromDraft(draft, hash, stored.Model);
            changed = true;
        }

        if (!changed)
        {
            return false;
        }

        existing.TranslationsJson = OccupationDayTranslations.Serialize(map);
        return true;
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

        draft = AttachFacts(draft, facts, forceCatalog: true);
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

        draft = OccupationDayBlocks.WithDerived(new OccupationDayDraft(
            facts.TitleNl,
            (row.Morning ?? "").Trim(),
            (row.Midday ?? "").Trim(),
            (row.Afternoon ?? "").Trim(),
            (row.Closing ?? "").Trim(),
            (row.Highlights ?? []).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(4).ToList(),
            (row.VariesNote ?? "").Trim(),
            row.Blocks ?? [],
            (row.Tasks ?? []).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(OccupationDayFacts.MaxTasks).ToList(),
            (row.Skills ?? []).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).Take(8).ToList()));
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
            Closing = draft.Closing,
            HighlightsJson = JsonSerializer.Serialize(draft.Highlights),
            BlocksJson = OccupationDayBlocks.Serialize(draft.Blocks),
            TasksJson = JsonSerializer.Serialize(draft.Tasks ?? []),
            SkillsJson = JsonSerializer.Serialize(draft.Skills ?? []),
            VariesNote = draft.VariesNote,
            SourceModel = Trim(string.IsNullOrWhiteSpace(model) ? OccupationDayInLifeOptions.DefaultModel : model, 80),
            GeneratedAtUtc = generatedAtUtc.Kind == DateTimeKind.Utc
                ? generatedAtUtc
                : DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc),
            ContentHash = hash,
            Locale = "nl",
            ThinSource = facts.IsThin,
            TranslationsJson = "{}"
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
        existing.Closing = draft.Closing;
        existing.HighlightsJson = JsonSerializer.Serialize(draft.Highlights);
        existing.BlocksJson = OccupationDayBlocks.Serialize(draft.Blocks);
        existing.TasksJson = JsonSerializer.Serialize(draft.Tasks ?? []);
        existing.SkillsJson = JsonSerializer.Serialize(draft.Skills ?? []);
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
            Closing = row.Closing,
            Highlights = highlights,
            VariesNote = row.VariesNote,
            Blocks = OccupationDayBlocks.Parse(row.BlocksJson).ToList(),
            Tasks = ReadLines(row.TasksJson),
            Skills = ReadLines(row.SkillsJson),
            SourceModel = row.SourceModel,
            GeneratedAtUtc = row.GeneratedAtUtc,
            ContentHash = row.ContentHash,
            Locale = row.Locale,
            ThinSource = row.ThinSource,
            Translations = OccupationDayTranslations.Parse(row.TranslationsJson)
        };
    }

    private static string Trim(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
