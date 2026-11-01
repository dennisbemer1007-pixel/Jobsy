using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class OccupationDayDagtekstenImporter
{
    private readonly JobsyDbContext _db;
    private readonly ILogger<OccupationDayDagtekstenImporter> _logger;

    public OccupationDayDagtekstenImporter(JobsyDbContext db, ILogger<OccupationDayDagtekstenImporter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OccupationDayDagtekstenImportResult> ImportAsync(
        OccupationDayDagtekstenDocument? document,
        CancellationToken cancellationToken = default)
    {
        if (document?.Mapping is null || document.Mapping.Count == 0)
        {
            return new OccupationDayDagtekstenImportResult(0, 0, 0, 0, 0);
        }

        var teksten = (document.Teksten ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(item => item.Id.Trim(), StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var rejected = 0;
        var mapped = 0;
        var dirty = 0;

        var existingRows = await _db.OccupationDayInLives.ToDictionaryAsync(
            item => item.EscoId,
            item => item,
            StringComparer.OrdinalIgnoreCase,
            cancellationToken);

        foreach (var row in document.Mapping)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var escoId = (row.EscoId ?? "").Trim().ToLowerInvariant();
            if (escoId.Length == 0)
            {
                rejected++;
                continue;
            }

            var facts = OccupationDayFacts.For(escoId);
            if (facts is null)
            {
                rejected++;
                continue;
            }

            var tekstId = (row.TekstId ?? "").Trim();
            if (tekstId.Length == 0 || !teksten.TryGetValue(tekstId, out var tekst))
            {
                rejected++;
                continue;
            }

            if (!OccupationDayDagtekstenImport.TryBuild(tekst, facts, out var dutch, out var english, out var error))
            {
                _logger.LogDebug("Dagteksten import afgekeurd voor {EscoId}: {Reason}", escoId, error);
                rejected++;
                continue;
            }

            mapped++;
            var hash = OccupationDayInLifeHash.Compute(facts.EscoId, dutch);
            var model = string.IsNullOrWhiteSpace(document.Model)
                ? OccupationDayDagtekstenImport.SourceModel
                : document.Model.Trim();
            var generatedAt = DateTime.UtcNow;

            existingRows.TryGetValue(facts.EscoId, out var existing);

            if (existing is not null && string.Equals(existing.ContentHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                if (MergeEnglish(existing, english, hash, model))
                {
                    updated++;
                    dirty++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            if (existing is null)
            {
                var entity = ToEntity(facts, dutch, hash, model, generatedAt);
                entity.TranslationsJson = EnglishJson(english, hash, model);
                _db.OccupationDayInLives.Add(entity);
                existingRows[facts.EscoId] = entity;
                inserted++;
            }
            else
            {
                Apply(existing, facts, dutch, hash, model, generatedAt);
                existing.TranslationsJson = EnglishJson(english, hash, model);
                updated++;
            }

            dirty++;
            if (dirty >= 250)
            {
                await _db.SaveChangesAsync(cancellationToken);
                dirty = 0;
            }
        }

        if (dirty > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new OccupationDayDagtekstenImportResult(inserted, updated, unchanged, rejected, mapped);
    }

    public static OccupationDayDagtekstenDocument? ParseDocument(Stream stream)
    {
        try
        {
            return JsonSerializer.Deserialize<OccupationDayDagtekstenDocument>(stream, OccupationDayJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool MergeEnglish(
        OccupationDayInLife existing,
        OccupationDayDraft english,
        string hash,
        string model)
    {
        var map = OccupationDayTranslations.Parse(existing.TranslationsJson);
        if (map.TryGetValue("en", out var stored)
            && string.Equals(stored.SourceHash, hash, StringComparison.OrdinalIgnoreCase)
            && string.Equals(stored.Morning, english.Morning, StringComparison.Ordinal))
        {
            return false;
        }

        var dutch = DraftFrom(existing);
        if (!OccupationDayInLifeValidator.TryValidateTranslation(dutch, english, out _))
        {
            return false;
        }

        map["en"] = OccupationDayTranslations.FromDraft(english, hash, model);
        existing.TranslationsJson = OccupationDayTranslations.Serialize(map);
        return true;
    }

    private static string EnglishJson(OccupationDayDraft english, string hash, string model)
    {
        var map = new Dictionary<string, OccupationDayStoredTranslation>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = OccupationDayTranslations.FromDraft(english, hash, model)
        };
        return OccupationDayTranslations.Serialize(map);
    }

    private static OccupationDayDraft DraftFrom(OccupationDayInLife row)
    {
        var blocks = OccupationDayBlocks.Parse(row.BlocksJson);
        return OccupationDayBlocks.WithDerived(new OccupationDayDraft(
            row.TitleNl,
            row.Morning,
            row.Midday,
            row.Afternoon,
            row.Closing,
            ReadList(row.HighlightsJson),
            row.VariesNote,
            blocks.ToList(),
            ReadList(row.TasksJson),
            ReadList(row.SkillsJson)));
    }

    private static List<string> ReadList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static OccupationDayInLife ToEntity(
        OccupationDayFacts facts,
        OccupationDayDraft draft,
        string hash,
        string model,
        DateTime generatedAtUtc)
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
            TasksJson = JsonSerializer.Serialize(facts.Tasks.Take(OccupationDayFacts.MaxTasks)),
            SkillsJson = JsonSerializer.Serialize(OccupationDaySkillsForKids.ToDisplayChips(facts.Skills, 8)),
            VariesNote = draft.VariesNote,
            SourceModel = Trim(model, 80),
            GeneratedAtUtc = generatedAtUtc,
            ContentHash = hash,
            Locale = "nl",
            ThinSource = facts.IsThin
        };

    private static void Apply(
        OccupationDayInLife row,
        OccupationDayFacts facts,
        OccupationDayDraft draft,
        string hash,
        string model,
        DateTime generatedAtUtc)
    {
        row.Uri = Trim(facts.Uri, 120);
        row.TitleNl = Trim(facts.TitleNl, 160);
        row.Morning = draft.Morning;
        row.Midday = draft.Midday;
        row.Afternoon = draft.Afternoon;
        row.Closing = draft.Closing;
        row.HighlightsJson = JsonSerializer.Serialize(draft.Highlights);
        row.BlocksJson = OccupationDayBlocks.Serialize(draft.Blocks);
        row.TasksJson = JsonSerializer.Serialize(facts.Tasks.Take(OccupationDayFacts.MaxTasks));
        row.SkillsJson = JsonSerializer.Serialize(OccupationDaySkillsForKids.ToDisplayChips(facts.Skills, 8));
        row.VariesNote = draft.VariesNote;
        row.SourceModel = Trim(model, 80);
        row.GeneratedAtUtc = generatedAtUtc;
        row.ContentHash = hash;
        row.Locale = "nl";
        row.ThinSource = facts.IsThin;
    }

    private static string Trim(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
