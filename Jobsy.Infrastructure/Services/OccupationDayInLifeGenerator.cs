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
/// Missing Dutch rows are filled once. Use <c>replaceExisting</c> to overwrite selected ids (regenerate).
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
        => GenerateMissingAsync(limit, skipEscoIds: null, onlyEscoIds: null, replaceExisting: false, cancellationToken);

    public async Task<OccupationDayGenerateResult> GenerateMissingAsync(
        int limit,
        IReadOnlySet<string>? skipEscoIds,
        IReadOnlySet<string>? onlyEscoIds,
        CancellationToken cancellationToken = default)
        => await GenerateMissingAsync(limit, skipEscoIds, onlyEscoIds, replaceExisting: false, cancellationToken);

    public async Task<OccupationDayGenerateResult> GenerateMissingAsync(
        int limit,
        IReadOnlySet<string>? skipEscoIds,
        IReadOnlySet<string>? onlyEscoIds,
        bool replaceExisting,
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
            .Where(job => replaceExisting || !complete.Contains(job.Id))
            .Where(job => skipEscoIds is null || !skipEscoIds.Contains(job.Id))
            .Take(limit)
            .ToList();
        var remaining = OccupationCatalog.Shared.All.Count(job =>
            (onlyEscoIds is null || onlyEscoIds.Contains(job.Id))
            && (replaceExisting || !complete.Contains(job.Id)));
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
                if (!replaceExisting
                    && OccupationDayTranslations.IsComplete(existing.TranslationsJson, existing.ContentHash))
                {
                    skipped++;
                    continue;
                }

                if (replaceExisting)
                {
                    var replaced = await ReplaceDutchAsync(existing, facts, failures, cancellationToken);
                    if (replaced is ReplaceOutcome.KeyMissing or ReplaceOutcome.KeyRejected)
                    {
                        return StopForKey(
                            generated,
                            skipped,
                            remaining,
                            failures,
                            replaced == ReplaceOutcome.KeyMissing,
                            replaced == ReplaceOutcome.KeyRejected);
                    }

                    if (replaced == ReplaceOutcome.Updated)
                    {
                        generated++;
                    }

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

            var (draft, error, keyMissing, keyRejected, model) = await ComposeAsync(facts, cancellationToken);
            if (keyMissing || keyRejected)
            {
                failures.Add(new OccupationDayFailure(facts.EscoId, error ?? (keyMissing ? OccupationDayWriteErrors.KeyMissing : OccupationDayWriteErrors.KeyInvalid)));
                return StopForKey(generated, skipped, remaining, failures, keyMissing, keyRejected);
            }

            if (draft is null)
            {
                _logger.LogWarning(
                    "Dag-in-het-leven mislukt voor beroep {EscoId}: {Reason}",
                    facts.EscoId,
                    OccupationDayWriteErrors.SafeSnippet(error));
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

    private enum ReplaceOutcome
    {
        Failed,
        Updated,
        KeyMissing,
        KeyRejected
    }

    private async Task<ReplaceOutcome> ReplaceDutchAsync(
        OccupationDayInLife existing,
        OccupationDayFacts facts,
        List<OccupationDayFailure> failures,
        CancellationToken cancellationToken)
    {
        var (draft, error, keyMissing, keyRejected, model) = await ComposeAsync(facts, cancellationToken);
        if (keyMissing)
        {
            failures.Add(new OccupationDayFailure(facts.EscoId, error ?? OccupationDayWriteErrors.KeyMissing));
            return ReplaceOutcome.KeyMissing;
        }

        if (keyRejected)
        {
            failures.Add(new OccupationDayFailure(facts.EscoId, error ?? OccupationDayWriteErrors.KeyInvalid));
            return ReplaceOutcome.KeyRejected;
        }

        if (draft is null)
        {
            _logger.LogWarning(
                "Dag-in-het-leven opnieuw genereren mislukt voor beroep {EscoId}: {Reason}",
                facts.EscoId,
                OccupationDayWriteErrors.SafeSnippet(error));
            failures.Add(new OccupationDayFailure(facts.EscoId, error ?? "afgekeurd"));
            return ReplaceOutcome.Failed;
        }

        var hash = OccupationDayInLifeHash.Compute(facts.EscoId, draft);
        Apply(existing, facts, draft, model, DateTime.UtcNow, hash);
        existing.TranslationsJson = "{}";
        await _db.SaveChangesAsync(cancellationToken);
        var after = await FillTranslationsAsync(existing, draft, failures, cancellationToken);
        return after == FillOutcome.KeyMissing ? ReplaceOutcome.KeyMissing : ReplaceOutcome.Updated;
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
        OccupationDayTranslateResult result;
        try
        {
            result = await _translator.TranslateAsync(dutch, language, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Vertaling dag-in-het-leven mislukt naar {Language}: {Reason}",
                language,
                OccupationDayWriteErrors.SafeSnippet(ex.Message));
            return new OccupationDayTranslateResult(false, null, OccupationDayWriteErrors.Timeout, "", false);
        }

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

    public async Task<OccupationDayProbeResult> ProbeAsync(string? escoId, CancellationToken cancellationToken = default)
    {
        var facts = OccupationDayFacts.For(escoId);
        if (facts is null)
        {
            return new OccupationDayProbeResult(false, escoId, null, "onbekend-beroep", null);
        }

        try
        {
            var (draft, error, _, _, model) = await ComposeAsync(facts, cancellationToken);
            return draft is not null
                ? new OccupationDayProbeResult(true, facts.EscoId, facts.TitleNl, null, model)
                : new OccupationDayProbeResult(false, facts.EscoId, facts.TitleNl, error ?? "afgekeurd", model);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = OccupationDayWriteErrors.SafeSnippet(ex.Message);
            _logger.LogWarning(
                "Test dag-in-het-leven mislukt voor beroep {EscoId}: {Reason}",
                facts.EscoId,
                reason);
            return new OccupationDayProbeResult(false, facts.EscoId, facts.TitleNl, OccupationDayWriteErrors.Timeout + ": " + reason, _options.Model);
        }
    }

    private static OccupationDayGenerateResult StopForKey(
        int generated,
        int skipped,
        int remaining,
        List<OccupationDayFailure> failures,
        bool keyMissing,
        bool keyRejected)
        => new(
            generated,
            failures.Count,
            skipped,
            Math.Max(0, remaining - generated),
            keyMissing,
            failures,
            keyRejected);

    private static bool LooksLikeProviderFailure(string? error)
        => OccupationDayWriteErrors.IsKeyInvalid(error)
           || string.Equals(error, OccupationDayWriteErrors.Timeout, StringComparison.Ordinal)
           || (error ?? "").StartsWith("openai-http", StringComparison.Ordinal)
           || string.Equals(error, OccupationDayWriteErrors.KeyMissing, StringComparison.Ordinal);

    private async Task<(OccupationDayDraft? Draft, string? Error, bool KeyMissing, bool KeyRejected, string Model)> ComposeAsync(
        OccupationDayFacts facts,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Clamp(_options.MaxComposeRetries, 2, 5);
        string? lastError = null;
        string model = _options.Model;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var user = attempt == 1
                ? facts.ToPrompt()
                : facts.ToPrompt() + "\n\n" + OccupationDayInLifePrompt.RetryFor(lastError);
            var result = await AskAsync(facts, user, cancellationToken);
            model = result.Model;
            if (result.Draft is not null || result.KeyMissing || result.KeyRejected || LooksLikeProviderFailure(result.Error))
            {
                return result;
            }

            lastError = result.Error;
            if (attempt < attempts && _options.DelayMilliseconds > 0)
            {
                await Task.Delay(_options.DelayMilliseconds, cancellationToken);
            }
        }

        _logger.LogWarning(
            "Dag-in-het-leven na {Attempts} pogingen afgekeurd voor beroep {EscoId}: {Reason}",
            attempts,
            facts.EscoId,
            OccupationDayWriteErrors.SafeSnippet(lastError));
        return (null, lastError ?? "afgekeurd", false, false, model);
    }

    private async Task<(OccupationDayDraft? Draft, string? Error, bool KeyMissing, bool KeyRejected, string Model)> AskAsync(
        OccupationDayFacts facts,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        OccupationDayWriteResult write;
        try
        {
            write = await _writer.CompleteAsync(OccupationDayInLifePrompt.System, userPrompt, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Dag-in-het-leven aanroep mislukt voor beroep {EscoId}: {Reason}",
                facts.EscoId,
                OccupationDayWriteErrors.SafeSnippet(ex.Message));
            return (null, OccupationDayWriteErrors.Timeout, false, false, _options.Model);
        }

        var model = string.IsNullOrWhiteSpace(write.Model) ? _options.Model : write.Model;
        if (write.KeyMissing)
        {
            return (null, write.Error, true, false, model);
        }

        if (OccupationDayWriteErrors.IsKeyInvalid(write.Error))
        {
            return (null, write.Error, false, true, model);
        }

        if (!write.Ok)
        {
            return (null, write.Error ?? OccupationDayWriteErrors.Http, false, false, model);
        }

        if (!OccupationDayInLifeJson.TryParse(write.Json, facts, out var draft, out var error))
        {
            return (null, error ?? "onleesbaar", false, false, model);
        }

        draft = AttachFacts(draft, facts, forceCatalog: true);
        if (!OccupationDayInLifeValidator.TryValidate(draft, facts, out var reasons))
        {
            return (null, string.Join("; ", reasons), false, false, model);
        }

        if (!OccupationDayQualityChecker.TryCheck(draft, facts, out var quality))
        {
            return (null, string.Join("; ", quality), false, false, model);
        }

        if (_options.QualityReviewWithAi)
        {
            var ai = await ReviewWithAiAsync(draft, facts, write.Json, cancellationToken);
            if (!ai.Ok)
            {
                return (null, ai.Reason ?? "kwaliteit", false, false, model);
            }
        }

        return (draft, null, false, false, model);
    }

    private async Task<(bool Ok, string? Reason)> ReviewWithAiAsync(
        OccupationDayDraft draft,
        OccupationDayFacts facts,
        string? dayJson,
        CancellationToken cancellationToken)
    {
        var payload = dayJson;
        if (string.IsNullOrWhiteSpace(payload))
        {
            payload = JsonSerializer.Serialize(new
            {
                blocks = (draft.Blocks ?? []).Select(block => new { block.Key, block.Label, block.Text }),
                highlights = draft.Highlights,
                varies = draft.VariesNote
            });
        }

        var source = facts.TitleNl + "\n" + facts.Description;
        foreach (var task in facts.Tasks.Take(4))
        {
            source += "\n- " + task;
        }

        OccupationDayWriteResult write;
        try
        {
            write = await _writer.CompleteAsync(
                OccupationDayQualityPrompt.System,
                OccupationDayQualityPrompt.UserMessage(facts.TitleNl, source, payload),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Kwaliteitscontrole dag-in-het-leven mislukt voor beroep {EscoId}: {Reason}",
                facts.EscoId,
                OccupationDayWriteErrors.SafeSnippet(ex.Message));
            return (true, null);
        }

        if (!write.Ok || string.IsNullOrWhiteSpace(write.Json))
        {
            return (true, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(write.Json);
            if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.ValueKind == JsonValueKind.True)
            {
                return (true, null);
            }

            var issues = new List<string>();
            if (doc.RootElement.TryGetProperty("issues", out var issuesProp) && issuesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in issuesProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            issues.Add(text.Trim());
                        }
                    }
                }
            }

            return issues.Count == 0
                ? (false, "kwaliteit-ai")
                : (false, string.Join("; ", issues));
        }
        catch (JsonException)
        {
            return (true, null);
        }
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
