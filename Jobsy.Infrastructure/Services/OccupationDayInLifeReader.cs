using System.Text.Json;
using Jobsy.Core.Careers;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

/// <summary>Reads a stored day. Does not call OpenAI.</summary>
public sealed class OccupationDayInLifeReader : IOccupationDayInLifeReader
{
    private readonly JobsyDbContext _db;
    private readonly OccupationDayInLifeOptions _options;

    public OccupationDayInLifeReader(JobsyDbContext db, IOptions<OccupationDayInLifeOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<OccupationDayReadResult> GetAsync(
        string? escoId,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var facts = OccupationDayFacts.For(escoId);
        if (facts is null)
        {
            return new OccupationDayReadResult(false, _options.Enabled, null);
        }

        if (!_options.Enabled)
        {
            return new OccupationDayReadResult(true, false, null);
        }

        var row = await _db.OccupationDayInLives.AsNoTracking()
            .FirstOrDefaultAsync(item => item.EscoId == facts.EscoId, cancellationToken);
        if (row is null)
        {
            return new OccupationDayReadResult(true, true, null);
        }

        return new OccupationDayReadResult(true, true, ToView(row, facts.TitleNl, language));
    }

    public async Task<OccupationDayAdminStatus> StatusAsync(
        bool running,
        int generatedThisRun,
        int failedThisRun,
        string? lastError,
        string? lastEscoId,
        CancellationToken cancellationToken = default)
    {
        var storedRows = await _db.OccupationDayInLives.AsNoTracking()
            .Select(item => new { item.EscoId, item.ContentHash, item.TranslationsJson })
            .ToListAsync(cancellationToken);
        var complete = storedRows
            .Where(item => OccupationDayTranslations.IsComplete(item.TranslationsJson, item.ContentHash))
            .Select(item => item.EscoId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var total = OccupationCatalog.Shared.All.Count;
        var remaining = OccupationCatalog.Shared.All.Count(job => !complete.Contains(job.Id));
        return new OccupationDayAdminStatus(
            storedRows.Count,
            total,
            remaining,
            running,
            generatedThisRun,
            failedThisRun,
            lastError,
            lastEscoId);
    }

    internal static OccupationDayView ToView(Jobsy.Core.Entities.OccupationDayInLife row, string title, string? language = null)
    {
        IReadOnlyList<string> highlights;
        try
        {
            highlights = JsonSerializer.Deserialize<List<string>>(row.HighlightsJson) ?? [];
        }
        catch (JsonException)
        {
            highlights = [];
        }

        var view = new OccupationDayView(
            row.EscoId,
            string.IsNullOrWhiteSpace(row.TitleNl) ? title : row.TitleNl,
            row.Morning,
            row.Midday,
            row.Afternoon,
            row.Closing,
            highlights,
            row.VariesNote,
            row.ThinSource);
        if (!OccupationDayTranslations.TryGet(row.TranslationsJson, language, row.ContentHash, out var translated))
        {
            return view;
        }

        return view with
        {
            TitleNl = string.IsNullOrWhiteSpace(translated.Title) ? view.TitleNl : translated.Title,
            Morning = translated.Morning,
            Midday = translated.Midday,
            Afternoon = translated.Afternoon,
            Closing = translated.Closing,
            Highlights = translated.Highlights ?? [],
            VariesNote = string.IsNullOrWhiteSpace(translated.Varies) ? view.VariesNote : translated.Varies
        };
    }
}
