using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class ContextualTipsService : IContextualTipsService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly JobsyDbContext _db;

    public ContextualTipsService(JobsyDbContext db) => _db = db;

    public async Task<ContextualTipsStateDto> GetStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await LoadAsync(userId, cancellationToken);
        var hidden = ParseHidden(row.HiddenTipIdsJson);
        return Map(row, hidden);
    }

    public async Task HideTipAsync(Guid userId, string tipId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tipId))
        {
            return;
        }

        var row = await LoadAsync(userId, cancellationToken, track: true);
        var hidden = ParseHidden(row.HiddenTipIdsJson);
        var id = tipId.Trim();
        if (hidden.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        hidden.Add(id);
        row.HiddenTipIdsJson = JsonSerializer.Serialize(hidden, Json);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreHiddenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await LoadAsync(userId, cancellationToken, track: true);
        if (row.HiddenTipIdsJson == "[]")
        {
            return;
        }

        row.HiddenTipIdsJson = "[]";
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContextualTipsStateDto> UpdateSettingsAsync(
        Guid userId,
        UpdateContextualTipsSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadAsync(userId, cancellationToken, track: true);
        if (request.MomentTipsEnabled is bool moment)
        {
            row.MomentTipsEnabled = moment;
        }

        if (request.InterviewPrepReminderEnabled is bool reminder)
        {
            row.InterviewPrepReminderEnabled = reminder;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(row, ParseHidden(row.HiddenTipIdsJson));
    }

    public async Task<bool> IsTipVisibleAsync(Guid userId, string tipId, CancellationToken cancellationToken = default)
    {
        var row = await LoadAsync(userId, cancellationToken);
        if (!row.MomentTipsEnabled)
        {
            return false;
        }

        var hidden = ParseHidden(row.HiddenTipIdsJson);
        return !hidden.Contains(tipId, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<CandidateContextualTipsSettings> LoadAsync(
        Guid userId,
        CancellationToken cancellationToken,
        bool track = false)
    {
        CandidateContextualTipsSettings? row;
        if (track)
        {
            row = await _db.CandidateContextualTipsSettings
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            if (row is null)
            {
                row = new CandidateContextualTipsSettings { UserId = userId };
                _db.CandidateContextualTipsSettings.Add(row);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return row;
        }

        row = await _db.CandidateContextualTipsSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        return row ?? new CandidateContextualTipsSettings { UserId = userId };
    }

    private static List<string> ParseHidden(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static ContextualTipsStateDto Map(CandidateContextualTipsSettings row, List<string> hidden)
        => new(row.MomentTipsEnabled, row.InterviewPrepReminderEnabled, hidden, hidden.Count);
}
