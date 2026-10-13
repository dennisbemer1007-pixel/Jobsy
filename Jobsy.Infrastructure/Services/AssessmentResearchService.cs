using System.Globalization;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AssessmentResearchService : IAssessmentResearchService
{
    private const int KAnonymityMinimum = 5;

    private readonly JobsyDbContext _db;
    private readonly IResearchConsentGate _gate;

    public AssessmentResearchService(JobsyDbContext db, IResearchConsentGate gate)
    {
        _db = db;
        _gate = gate;
    }

    public Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default)
        => _gate.IsEnabledAsync(cancellationToken);

    public async Task AcceptResearchConsentAsync(
        Guid userId,
        ResearchDemographicsDto? demographics,
        CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsEnabledAsync(cancellationToken))
        {
            throw new InvalidOperationException("Onderzoeksdeelname is momenteel niet beschikbaar.");
        }

        var user = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        user.ResearchConsentAt = DateTime.UtcNow;
        user.ResearchConsentVersion = PrivacyConstants.ResearchConsentTextVersion;
        ApplyDemographics(user, demographics);
        await _db.SaveChangesAsync(cancellationToken);
        await BackfillConsentedUserAsync(userId, cancellationToken);
    }

    public async Task WithdrawResearchConsentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        user.ResearchConsentAt = null;
        user.ResearchConsentVersion = null;
        user.ResearchAgeBand = null;
        user.ResearchGender = null;
        user.ResearchEducationLevel = null;
        user.ResearchNativeLanguage = null;
        user.ResearchTestLanguage = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDemographicsAsync(
        Guid userId,
        ResearchDemographicsDto demographics,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        if (!ResearchConsentRules.DemographicsAllowed(user))
        {
            throw new InvalidOperationException("Demografie is alleen beschikbaar na onderzoekstoestemming.");
        }

        ApplyDemographics(user, demographics);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordAccountAttemptAsync(
        Guid userId,
        AssessmentKind kind,
        int testVersion,
        string testLanguage,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        int? totalDurationMs,
        CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsEnabledAsync(cancellationToken))
        {
            return;
        }

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        if (!ResearchConsentRules.HasCurrentResearchConsent(user))
        {
            return;
        }

        if (answers.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var attempt = new AssessmentResearchAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RespondentHash = ResearchRespondentHash.ForUser(userId, now),
            TestKey = ResearchTestKeys.QuickScanExportKey(kind),
            TestVersion = AssessmentTestVersions.Normalize(testVersion),
            TestLanguage = NormalizeLanguage(testLanguage),
            AgeBand = user.ResearchAgeBand,
            Gender = user.ResearchGender,
            EducationLevel = user.ResearchEducationLevel,
            NativeLanguage = user.ResearchNativeLanguage,
            Status = 2,
            CompletedAtUtc = now,
            TotalDurationMs = totalDurationMs,
            IsPilot = false
        };

        foreach (var (itemId, raw) in answers)
        {
            int? ms = null;
            if (itemDurationMs?.TryGetValue(itemId, out var duration) == true)
            {
                ms = duration;
            }

            attempt.Items.Add(new AssessmentResearchItemResponse
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ItemId = itemId,
                ResponseRaw = raw,
                DurationMs = ms
            });
        }

        _db.AssessmentResearchAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task BackfillConsentedUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsEnabledAsync(cancellationToken))
        {
            return;
        }

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        if (!ResearchConsentRules.HasCurrentResearchConsent(user))
        {
            return;
        }

        var language = NormalizeLanguage(user.ResearchTestLanguage ?? ParseUiLanguage(user.PreferencesJson));

        await BackfillKindAsync(userId, AssessmentKind.Competence, language, cancellationToken);
        await BackfillKindAsync(userId, AssessmentKind.Career, language, cancellationToken);
        await BackfillKindAsync(userId, AssessmentKind.Culture, language, cancellationToken);
        await BackfillKindAsync(userId, AssessmentKind.Values, language, cancellationToken);
    }

    public async Task<ResearchPilotStartDto?> StartPilotAsync(
        string campaignCode,
        string? testLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsEnabledAsync(cancellationToken))
        {
            return null;
        }

        var code = campaignCode.Trim().ToUpperInvariant();
        var campaign = await _db.ResearchPilotCampaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code && c.IsActive, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        var session = new ResearchPilotSession
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            RespondentHash = ResearchRespondentHash.RandomPilot(),
            TestLanguage = NormalizeLanguage(testLanguage),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ResearchPilotSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return new ResearchPilotStartDto(session.Id, campaign.Title, session.RespondentHash);
    }

    public async Task RecordPilotAttemptAsync(
        Guid pilotSessionId,
        AssessmentKind kind,
        int testVersion,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        IReadOnlyDictionary<int, bool>? itemUnclear,
        int? totalDurationMs,
        string? endComment,
        CancellationToken cancellationToken = default)
    {
        if (!await _gate.IsEnabledAsync(cancellationToken))
        {
            throw new InvalidOperationException("Pilot is niet actief.");
        }

        var session = await _db.ResearchPilotSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == pilotSessionId, cancellationToken)
            ?? throw new InvalidOperationException("Pilot-sessie niet gevonden.");

        var now = DateTime.UtcNow;
        var attempt = new AssessmentResearchAttempt
        {
            Id = Guid.NewGuid(),
            PilotSessionId = session.Id,
            RespondentHash = session.RespondentHash,
            TestKey = ResearchTestKeys.QuickScanExportKey(kind),
            TestVersion = AssessmentTestVersions.Normalize(testVersion),
            TestLanguage = session.TestLanguage,
            AgeBand = session.AgeBand,
            Gender = session.Gender,
            EducationLevel = session.EducationLevel,
            NativeLanguage = session.NativeLanguage,
            Status = 2,
            CompletedAtUtc = now,
            TotalDurationMs = totalDurationMs,
            PilotEndComment = TrimComment(endComment),
            IsPilot = true
        };

        foreach (var (itemId, raw) in answers)
        {
            int? ms = null;
            if (itemDurationMs?.TryGetValue(itemId, out var duration) == true)
            {
                ms = duration;
            }

            var unclear = itemUnclear?.TryGetValue(itemId, out var flag) == true && flag;
            attempt.Items.Add(new AssessmentResearchItemResponse
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ItemId = itemId,
                ResponseRaw = raw,
                DurationMs = ms,
                ItemUnclear = unclear
            });
        }

        _db.AssessmentResearchAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ResearchDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var consentedUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.ResearchConsentAt != null
                        && u.ResearchConsentVersion == PrivacyConstants.ResearchConsentTextVersion)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var attempts = await _db.AssessmentResearchAttempts.AsNoTracking()
            .Where(a => !a.IsPilot && a.UserId != null && consentedUserIds.Contains(a.UserId.Value))
            .Select(a => new { a.TestKey, a.TestVersion, a.TestLanguage, a.RespondentHash })
            .ToListAsync(cancellationToken);

        var grouped = attempts
            .GroupBy(a => (a.TestKey, a.TestVersion, a.TestLanguage))
            .Select(g => new
            {
                g.Key.TestKey,
                g.Key.TestVersion,
                g.Key.TestLanguage,
                Count = g.Select(x => x.RespondentHash).Distinct().Count()
            })
            .OrderBy(x => x.TestKey)
            .ThenBy(x => x.TestVersion)
            .ThenBy(x => x.TestLanguage)
            .ToList();

        var rows = grouped.Select(g => new ResearchDashboardRowDto(
            g.TestKey,
            g.TestVersion,
            g.TestLanguage,
            g.Count < KAnonymityMinimum ? 0 : g.Count,
            PrivacyConstants.ResearchExportTargetRespondents,
            g.Count is > 0 and < KAnonymityMinimum)).ToList();

        return new ResearchDashboardDto(rows, PrivacyConstants.ResearchExportTargetRespondents);
    }

    public async Task<byte[]> ExportCsvAsync(
        string? testKey,
        int? testVersion,
        CancellationToken cancellationToken = default)
    {
        var consentedUserIds = await _db.Users.AsNoTracking()
            .Where(u => u.ResearchConsentAt != null
                        && u.ResearchConsentVersion == PrivacyConstants.ResearchConsentTextVersion)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var query = _db.AssessmentResearchItemResponses.AsNoTracking()
            .Include(i => i.Attempt)
            .Where(i => i.Attempt != null
                        && !i.Attempt.IsPilot
                        && i.Attempt.UserId != null
                        && consentedUserIds.Contains(i.Attempt.UserId.Value));

        if (!string.IsNullOrWhiteSpace(testKey))
        {
            var key = testKey.Trim().ToLowerInvariant();
            query = query.Where(i => i.Attempt!.TestKey == key);
        }

        if (testVersion is int version)
        {
            query = query.Where(i => i.Attempt!.TestVersion == version);
        }

        var rows = await query
            .OrderBy(i => i.Attempt!.TestKey)
            .ThenBy(i => i.Attempt!.RespondentHash)
            .ThenBy(i => i.ItemId)
            .Select(i => new ExportRow(
                i.Attempt!.RespondentHash,
                i.Attempt.TestLanguage,
                i.Attempt.Gender ?? "unknown",
                i.Attempt.AgeBand ?? "unknown",
                i.Attempt.TestKey,
                i.Attempt.TestVersion,
                i.ItemId,
                i.ResponseRaw,
                i.Attempt.Status,
                i.Attempt.CompletedAtUtc,
                i.Attempt.TotalDurationMs))
            .ToListAsync(cancellationToken);

        ApplyKAnonymity(rows);

        var sb = new StringBuilder();
        sb.AppendLine("respondent_hash,language,gender,age_band,test_key,test_version,item_id,response_raw,status,completed_at,duration_ms");
        foreach (var row in rows)
        {
            sb.Append(Csv(row.RespondentHash)).Append(',');
            sb.Append(Csv(row.Language)).Append(',');
            sb.Append(Csv(row.Gender)).Append(',');
            sb.Append(Csv(row.AgeBand)).Append(',');
            sb.Append(Csv(row.TestKey)).Append(',');
            sb.Append(row.TestVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(row.ItemId.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(row.ResponseRaw.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(row.Status.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(row.CompletedAtUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(row.DurationMs?.ToString(CultureInfo.InvariantCulture) ?? "");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<ResearchPilotCampaignDto> CreatePilotCampaignAsync(
        string title,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var code = await GenerateUniquePilotCodeAsync(cancellationToken);
        var campaign = new ResearchPilotCampaign
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = string.IsNullOrWhiteSpace(title) ? "Pilot" : title.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = adminUserId
        };
        _db.ResearchPilotCampaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);

        return ToPilotDto(campaign);
    }

    public async Task<IReadOnlyList<ResearchPilotCampaignDto>> ListPilotCampaignsAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _db.ResearchPilotCampaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return list.Select(ToPilotDto).ToList();
    }

    private async Task BackfillKindAsync(
        Guid userId,
        AssessmentKind kind,
        string language,
        CancellationToken cancellationToken)
    {
        var (answers, completedAt) = await LoadCompletedAnswersAsync(userId, kind, cancellationToken);
        if (answers is null || completedAt is null || answers.Count == 0)
        {
            return;
        }

        var testKey = ResearchTestKeys.QuickScanExportKey(kind);
        var exists = await _db.AssessmentResearchAttempts.AsNoTracking()
            .AnyAsync(
                a => a.UserId == userId
                     && a.TestKey == testKey
                     && a.CompletedAtUtc == completedAt,
                cancellationToken);
        if (exists)
        {
            return;
        }

        await RecordAccountAttemptAsync(
            userId,
            kind,
            AssessmentTestVersions.V1,
            language,
            answers,
            itemDurationMs: null,
            totalDurationMs: null,
            cancellationToken);
    }

    private async Task<(Dictionary<int, int>? Answers, DateTime? CompletedAt)> LoadCompletedAnswersAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case AssessmentKind.Career:
            {
                var row = await _db.CandidateCareerInterests.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                if (row is null || row.CompletedAtUtc is null)
                {
                    return (null, null);
                }

                return (CareerTestCatalog.ParseAnswersJson(row.AnswersJson), row.CompletedAtUtc);
            }
            case AssessmentKind.Culture:
            {
                var row = await _db.CandidateCulturePersonalityProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                if (row is null || row.CompletedAtUtc is null)
                {
                    return (null, null);
                }

                return (CulturePersonalityCatalog.ParseAnswers(row.AnswersJson).ToDictionary(kv => kv.Key, kv => kv.Value), row.CompletedAtUtc);
            }
            case AssessmentKind.Values:
            {
                var row = await _db.CandidateValuesProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                if (row is null || row.CompletedAtUtc is null)
                {
                    return (null, null);
                }

                return (SchwartzValuesCatalog.ParseAnswers(row.AnswersJson).ToDictionary(kv => kv.Key, kv => kv.Value), row.CompletedAtUtc);
            }
            default:
            {
                var row = await _db.CandidateCompetencies.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                if (row is null || row.CompletedAtUtc is null)
                {
                    return (null, null);
                }

                return (CompetencyTestCatalog.ParseAnswersJson(row.AnswersJson), row.CompletedAtUtc);
            }
        }
    }

    private static void ApplyDemographics(User user, ResearchDemographicsDto? demographics)
    {
        if (demographics is null)
        {
            return;
        }

        user.ResearchAgeBand = ResearchDemographicsCatalog.NormalizeBand(
            demographics.AgeBand,
            ResearchDemographicsCatalog.AgeBands);
        user.ResearchGender = ResearchDemographicsCatalog.NormalizeBand(
            demographics.Gender,
            ResearchDemographicsCatalog.Genders);
        user.ResearchEducationLevel = ResearchDemographicsCatalog.NormalizeBand(
            demographics.EducationLevel,
            ResearchDemographicsCatalog.EducationLevels);
        user.ResearchNativeLanguage = ResearchDemographicsCatalog.NormalizeBand(
            demographics.NativeLanguage,
            ResearchDemographicsCatalog.Languages);
        user.ResearchTestLanguage = ResearchDemographicsCatalog.NormalizeBand(
            demographics.TestLanguage,
            ResearchDemographicsCatalog.Languages)
            ?? user.ResearchTestLanguage;
    }

    private static string NormalizeLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "nl";
        }

        var lang = value.Trim().ToLowerInvariant();
        return lang.Length > 8 ? lang[..8] : lang;
    }

    private static string ParseUiLanguage(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return "nl";
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("language", out var lang))
            {
                return NormalizeLanguage(lang.GetString());
            }
        }
        catch
        {
            // ignore malformed preferences
        }

        return "nl";
    }

    private static string? TrimComment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 2000 ? trimmed : trimmed[..2000];
    }

    private async Task<string> GenerateUniquePilotCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4))
                .ToUpperInvariant();
            var exists = await _db.ResearchPilotCampaigns.AnyAsync(c => c.Code == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Kon geen unieke pilotcode genereren.");
    }

    private ResearchPilotCampaignDto ToPilotDto(ResearchPilotCampaign campaign)
        => new(
            campaign.Id,
            campaign.Code,
            campaign.Title,
            campaign.IsActive,
            campaign.CreatedAtUtc,
            $"/pilot/{campaign.Code}");

    private static void ApplyKAnonymity(List<ExportRow> rows)
    {
        var smallBuckets = rows
            .GroupBy(r => (r.TestKey, r.TestVersion, r.Language, r.Gender, r.AgeBand))
            .Where(g => g.Select(x => x.RespondentHash).Distinct().Count() is > 0 and < KAnonymityMinimum)
            .Select(g => g.Key)
            .ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (smallBuckets.Contains((row.TestKey, row.TestVersion, row.Language, row.Gender, row.AgeBand)))
            {
                rows[i] = row with { Gender = "suppressed", AgeBand = "suppressed" };
            }
        }
    }

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private sealed record ExportRow(
        string RespondentHash,
        string Language,
        string Gender,
        string AgeBand,
        string TestKey,
        int TestVersion,
        int ItemId,
        int ResponseRaw,
        int Status,
        DateTime CompletedAtUtc,
        int? DurationMs);
}
