using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyMapInitiativeService : ICompanyMapInitiativeService
{
    private static readonly TimeSpan EmailCodeTtl = TimeSpan.FromMinutes(15);

    private readonly JobsyDbContext _db;
    private readonly ITransactionalMailer _mailer;
    private readonly ITokenLedgerService _tokens;
    private readonly IFlexCommercialService _commercial;
    private readonly ILogger<CompanyMapInitiativeService> _logger;

    public CompanyMapInitiativeService(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        ITokenLedgerService tokens,
        IFlexCommercialService commercial,
        ILogger<CompanyMapInitiativeService> logger)
    {
        _db = db;
        _mailer = mailer;
        _tokens = tokens;
        _commercial = commercial;
        _logger = logger;
    }

    public async Task<PublicCompanyInitiativeViewDto?> GetPublicProfileByKvkAsync(
        string kvk,
        Guid? candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = CompanyPublicPaths.NormalizeKvkNumber(kvk);
        if (normalized is null)
        {
            return null;
        }

        var root = await _db.Companies.AsNoTracking()
            .Where(PublicVisibility.CompanyIsPublic)
            .Where(c => c.KvkNumber == normalized && c.ParentCompanyId == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
        {
            return null;
        }

        var settings = await _db.CompanyInitiativeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.RootCompanyId == root.Id, cancellationToken);
        var teams = await _db.InitiativeTeams.AsNoTracking()
            .Where(t => t.RootCompanyId == root.Id && t.IsOpen && t.ContactEmailConfirmedAtUtc != null)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var open = (settings?.IsEnabled == true && settings.ContactEmailConfirmedAtUtc != null) || teams.Count > 0;
        if (!open)
        {
            return null;
        }

        CandidateInitiativeWeekDto? week = null;
        if (candidateUserId is Guid uid)
        {
            week = await GetCandidateWeekAsync(uid, cancellationToken);
        }

        var photos = ParsePhotoUrls(root.HubPhotoUrlsJson);
        return new PublicCompanyInitiativeViewDto(
            root.Id,
            settings?.IsEnabled == true && settings.ContactEmailConfirmedAtUtc != null,
            teams.Count,
            root.HubAboutText,
            root.HubCultureText,
            root.HubVideoUrl,
            photos,
            teams.Select(t => new PublicInitiativeTeamOptionDto(
                t.Id,
                t.Name,
                t.Description,
                null,
                candidateUserId is null ? null : InitiativeMatchWordingRules.FromInternalScore(72),
                t.IsOpen)).ToList(),
            week);
    }

    public async Task<IReadOnlyList<CompanyMapPinDto>> GetMapPinsAsync(
        double? originLat,
        double? originLng,
        int radiusKm,
        Guid? candidateUserId,
        CancellationToken cancellationToken = default)
    {
        radiusKm = radiusKm switch
        {
            <= 10 => 10,
            <= 25 => 25,
            _ => 40
        };

        var companies = await _db.Companies.AsNoTracking()
            .Where(PublicVisibility.CompanyIsPublic)
            .Where(c => c.KvkVerificationStatus == KvkVerificationStatus.Verified)
            .Where(c => c.PublicPageBlockedAtUtc == null)
            .Where(c => c.Location != null)
            .Select(c => new
            {
                c.Id,
                c.ParentCompanyId,
                c.Name,
                c.Type,
                c.KvkNumber,
                c.KvkEstablishmentId,
                c.Address,
                Lat = c.Location!.Latitude,
                Lng = c.Location!.Longitude,
                c.InitiativeAvgResponseDays
            })
            .ToListAsync(cancellationToken);

        var rootIds = companies
            .Select(c => c.ParentCompanyId ?? c.Id)
            .Distinct()
            .ToList();

        var settings = await _db.CompanyInitiativeSettings.AsNoTracking()
            .Where(s => rootIds.Contains(s.RootCompanyId) && s.IsEnabled && s.ContactEmailConfirmedAtUtc != null)
            .Select(s => s.RootCompanyId)
            .ToListAsync(cancellationToken);

        var openTeams = await _db.InitiativeTeams.AsNoTracking()
            .Where(t => rootIds.Contains(t.RootCompanyId) && t.IsOpen && t.ContactEmailConfirmedAtUtc != null)
            .GroupBy(t => t.RootCompanyId)
            .Select(g => new { RootId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vacancyCounts = await _db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active)
            .GroupBy(v => v.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vacancyByCompany = vacancyCounts.ToDictionary(x => x.CompanyId, x => x.Count);
        var teamsByRoot = openTeams.ToDictionary(x => x.RootId, x => x.Count);
        var openRoots = settings.ToHashSet();

        static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        var pins = new List<CompanyMapPinDto>();
        foreach (var c in companies)
        {
            if (originLat is double olat && originLng is double olng)
            {
                var dist = HaversineKm(olat, olng, c.Lat, c.Lng);
                if (dist > radiusKm)
                {
                    continue;
                }
            }

            var rootId = c.ParentCompanyId ?? c.Id;
            var openForInitiative = openRoots.Contains(rootId);
            var teamCount = teamsByRoot.GetValueOrDefault(rootId);
            vacancyByCompany.TryGetValue(c.Id, out var vacCount);

            var path = CompanyPublicPaths.TryBuildPath(c.KvkNumber, c.KvkEstablishmentId);
            pins.Add(new CompanyMapPinDto(
                c.Id,
                rootId,
                c.Name,
                null,
                ExtractCity(c.Address),
                c.Lat,
                c.Lng,
                openForInitiative || teamCount > 0,
                c.Type == CompanyType.Intermediary,
                vacCount,
                teamCount,
                candidateUserId is null ? null : InitiativeMatchWordingRules.FromInternalScore(70),
                c.InitiativeAvgResponseDays,
                c.KvkNumber,
                path));
        }

        return pins;
    }

    public async Task<CandidateInitiativeWeekDto> GetCandidateWeekAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var weekStart = InitiativeWeekLimitRules.WeekStartAmsterdam(DateTime.UtcNow);
        var used = await _db.WorkInitiatives.AsNoTracking()
            .Where(i => i.CandidateUserId == candidateUserId
                        && i.CountsTowardWeeklyLimit
                        && i.CreatedAtUtc >= weekStart)
            .CountAsync(cancellationToken);

        return new CandidateInitiativeWeekDto(
            used,
            InitiativeWeekLimitRules.CandidateMaxPerWeek,
            InitiativeWeekLimitRules.NextWeekStartAmsterdam(DateTime.UtcNow));
    }

    public async Task<SendWorkInitiativeResult> SendAsync(
        Guid candidateUserId,
        SendWorkInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivation) || request.Motivation.Length > 500)
        {
            return new SendWorkInitiativeResult(false, false, null, null, "motivation_invalid");
        }

        var week = await GetCandidateWeekAsync(candidateUserId, cancellationToken);
        if (!InitiativeWeekLimitRules.CanSendAnother(week.Used))
        {
            await UpsertDraftAsync(candidateUserId, request, cancellationToken);
            return new SendWorkInitiativeResult(false, true, null, week, "week_limit");
        }

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == candidateUserId, cancellationToken);
        var created = DateTime.UtcNow;
        var initiative = new WorkInitiative
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateUserId,
            RootCompanyId = request.RootCompanyId,
            BranchCompanyId = request.BranchCompanyId,
            InitiativeTeamId = request.InitiativeTeamId,
            Motivation = request.Motivation.Trim(),
            MatchLabel = InitiativeMatchWordingRules.FromInternalScore(72),
            SnapshotRoleTitle = "Functie",
            SnapshotRegionLabel = "Regio",
            SnapshotWorkDnaSummary = "Werk-DNA snapshot",
            SnapshotAvailabilitySummary = "Beschikbaar",
            CandidateDisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            CandidateEmail = user.Email ?? "",
            SnapshotAttachedDocument = request.AttachPassport ? "passport" : "lobsy-cv",
            ResponseDeadlineUtc = InitiativeBusinessDayRules.AddBusinessDaysUtc(created, InitiativeBusinessDayRules.ResponseBusinessDays),
            CreatedAtUtc = created
        };

        _db.WorkInitiatives.Add(initiative);
        await _db.SaveChangesAsync(cancellationToken);

        week = await GetCandidateWeekAsync(candidateUserId, cancellationToken);
        return new SendWorkInitiativeResult(true, false, initiative.Id, week, null);
    }

    public async Task<SendWorkInitiativeResult> SendDraftAsync(
        Guid candidateUserId,
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _db.WorkInitiativeDrafts.FirstOrDefaultAsync(
            d => d.Id == draftId && d.CandidateUserId == candidateUserId,
            cancellationToken);
        if (draft is null)
        {
            return new SendWorkInitiativeResult(false, false, null, null, "draft_not_found");
        }

        var result = await SendAsync(
            candidateUserId,
            new SendWorkInitiativeRequest(
                draft.RootCompanyId,
                draft.BranchCompanyId,
                draft.InitiativeTeamId,
                draft.Motivation,
                AttachPassport: true),
            cancellationToken);

        if (result.Sent)
        {
            _db.WorkInitiativeDrafts.Remove(draft);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<WorkInitiativeListItemDto>> ListForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkInitiatives.AsNoTracking()
            .Where(i => i.CandidateUserId == candidateUserId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new WorkInitiativeListItemDto(
                i.Id,
                i.RootCompany.Name,
                i.InitiativeTeam != null ? i.InitiativeTeam.Name : null,
                i.Status,
                i.MatchLabel,
                i.CreatedAtUtc,
                i.ResponseDeadlineUtc,
                i.RespondedAtUtc,
                i.EmployerViewedAtUtc,
                true))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployerInitiativeInboxItemDto>> ListForEmployerAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var root = await ResolveRootCompanyIdAsync(companyId, cancellationToken);
        var rows = await _db.WorkInitiatives.AsNoTracking()
            .Where(i => i.RootCompanyId == root && i.Status != WorkInitiativeStatus.Withdrawn)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new
            {
                i.Id,
                i.CandidateUserId,
                i.SnapshotRoleTitle,
                i.SnapshotRegionLabel,
                i.SnapshotAvailabilitySummary,
                i.SnapshotHoursPerWeek,
                i.MatchLabel,
                i.SnapshotWorkDnaSummary,
                i.Status,
                i.CreatedAtUtc,
                i.ResponseDeadlineUtc,
                i.Motivation,
                i.CandidateDisplayName,
                i.CandidateEmail
            })
            .ToListAsync(cancellationToken);

        return rows.Select(i =>
        {
            var pii = InitiativeVisibilityRules.EmployerMaySeePii(i.Status);
            return new EmployerInitiativeInboxItemDto(
                i.Id,
                InitiativeCandidateDisplay.EmployerListLabel(i.CandidateUserId),
                i.SnapshotRoleTitle,
                i.SnapshotRegionLabel,
                i.SnapshotAvailabilitySummary,
                i.SnapshotHoursPerWeek,
                i.MatchLabel,
                i.SnapshotWorkDnaSummary,
                i.Status,
                i.CreatedAtUtc,
                i.ResponseDeadlineUtc,
                pii,
                pii ? i.Motivation : null,
                pii ? i.CandidateDisplayName : null,
                pii ? i.CandidateEmail : null);
        }).ToList();
    }

    public async Task<CompanyInitiativeSettingsDto?> GetSettingsAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyInitiativeSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.RootCompanyId == rootCompanyId, cancellationToken);
        var teams = await _db.InitiativeTeams.AsNoTracking()
            .Where(t => t.RootCompanyId == rootCompanyId)
            .OrderBy(t => t.Name)
            .Select(t => new InitiativeTeamDto(
                t.Id,
                t.Name,
                t.Description,
                t.BranchCompanyId,
                null,
                t.ContactEmail,
                t.ContactEmailConfirmedAtUtc != null,
                t.MaxPerWeek,
                t.IsOpen))
            .ToListAsync(cancellationToken);

        if (row is null && teams.Count == 0)
        {
            return null;
        }

        return new CompanyInitiativeSettingsDto(
            row?.IsEnabled ?? false,
            row?.ContactEmail ?? "",
            row?.ContactEmailConfirmedAtUtc != null,
            row?.MaxPerWeek ?? 5,
            teams);
    }

    public async Task<CompanyInitiativeSettingsDto> SaveSettingsAsync(
        Guid rootCompanyId,
        CompanyInitiativeSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyInitiativeSettings.FirstOrDefaultAsync(
            s => s.RootCompanyId == rootCompanyId,
            cancellationToken);
        if (row is null)
        {
            row = new CompanyInitiativeSettings
            {
                Id = Guid.NewGuid(),
                RootCompanyId = rootCompanyId
            };
            _db.CompanyInitiativeSettings.Add(row);
        }

        var emailChanged = !string.Equals(row.ContactEmail, dto.ContactEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        row.IsEnabled = dto.IsEnabled;
        row.ContactEmail = dto.ContactEmail.Trim();
        row.MaxPerWeek = Math.Clamp(dto.MaxPerWeek, 1, 50);
        row.UpdatedAtUtc = DateTime.UtcNow;

        if (emailChanged)
        {
            row.ContactEmailConfirmedAtUtc = null;
            await IssueEmailCodeAsync(row.ContactEmail, hash => row.ContactEmailConfirmationCodeHash = hash,
                exp => row.ContactEmailConfirmationExpiresAtUtc = exp);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetSettingsAsync(rootCompanyId, cancellationToken))!;
    }

    public async Task<InitiativeTeamDto> SaveTeamAsync(
        Guid rootCompanyId,
        InitiativeTeamDto team,
        CancellationToken cancellationToken = default)
    {
        if (team.Description.Length > 140)
        {
            throw new ArgumentException("Teamomschrijving max 140 tekens.");
        }

        InitiativeTeam entity;
        if (team.Id == Guid.Empty)
        {
            entity = new InitiativeTeam { Id = Guid.NewGuid(), RootCompanyId = rootCompanyId };
            _db.InitiativeTeams.Add(entity);
        }
        else
        {
            entity = await _db.InitiativeTeams.FirstAsync(
                t => t.Id == team.Id && t.RootCompanyId == rootCompanyId,
                cancellationToken);
        }

        var emailChanged = !string.Equals(entity.ContactEmail, team.ContactEmail.Trim(), StringComparison.OrdinalIgnoreCase);
        entity.Name = team.Name.Trim();
        entity.Description = team.Description.Trim();
        entity.BranchCompanyId = team.BranchCompanyId;
        entity.ContactEmail = team.ContactEmail.Trim();
        entity.MaxPerWeek = Math.Clamp(team.MaxPerWeek, 1, 50);
        entity.IsOpen = team.IsOpen && !string.IsNullOrWhiteSpace(team.ContactEmail);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.IsOpen && entity.ContactEmailConfirmedAtUtc is null)
        {
            throw new InvalidOperationException("team_email_required");
        }

        if (emailChanged)
        {
            entity.ContactEmailConfirmedAtUtc = null;
            entity.IsOpen = false;
            await IssueEmailCodeAsync(entity.ContactEmail, hash => entity.ContactEmailConfirmationCodeHash = hash,
                exp => entity.ContactEmailConfirmationExpiresAtUtc = exp);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new InitiativeTeamDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.BranchCompanyId,
            null,
            entity.ContactEmail,
            entity.ContactEmailConfirmedAtUtc != null,
            entity.MaxPerWeek,
            entity.IsOpen);
    }

    public Task ResendTeamEmailConfirmationAsync(Guid rootCompanyId, Guid teamId, CancellationToken cancellationToken = default)
        => ConfirmResendTeamAsync(rootCompanyId, teamId, cancellationToken);

    public async Task ResendGeneralEmailConfirmationAsync(Guid rootCompanyId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyInitiativeSettings.FirstAsync(s => s.RootCompanyId == rootCompanyId, cancellationToken);
        await IssueEmailCodeAsync(row.ContactEmail, hash => row.ContactEmailConfirmationCodeHash = hash,
            exp => row.ContactEmailConfirmationExpiresAtUtc = exp);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmTeamEmailAsync(Guid teamId, string code, CancellationToken cancellationToken = default)
    {
        var team = await _db.InitiativeTeams.FirstAsync(t => t.Id == teamId, cancellationToken);
        if (!VerifyCode(team.ContactEmailConfirmationCodeHash, team.ContactEmailConfirmationExpiresAtUtc, code))
        {
            team.ContactEmailConfirmationFailedAttempts++;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("code_invalid");
        }

        team.ContactEmailConfirmedAtUtc = DateTime.UtcNow;
        team.ContactEmailConfirmationCodeHash = null;
        team.ContactEmailConfirmationExpiresAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmGeneralEmailAsync(Guid rootCompanyId, string code, CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyInitiativeSettings.FirstAsync(s => s.RootCompanyId == rootCompanyId, cancellationToken);
        if (!VerifyCode(row.ContactEmailConfirmationCodeHash, row.ContactEmailConfirmationExpiresAtUtc, code))
        {
            row.ContactEmailConfirmationFailedAttempts++;
            await _db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("code_invalid");
        }

        row.ContactEmailConfirmedAtUtc = DateTime.UtcNow;
        row.ContactEmailConfirmationCodeHash = null;
        row.ContactEmailConfirmationExpiresAtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Ok, string? ErrorCode, decimal? Balance)> AcceptAsync(
        Guid employerUserId,
        Guid initiativeId,
        Guid billingCompanyId,
        CancellationToken cancellationToken = default)
    {
        var initiative = await _db.WorkInitiatives.FirstAsync(i => i.Id == initiativeId, cancellationToken);
        if (initiative.Status is WorkInitiativeStatus.Accepted or WorkInitiativeStatus.Withdrawn)
        {
            return (false, "react_failed", null);
        }

        var commercial = await _commercial.GetAsync(cancellationToken);
        var cost = AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            VacancyKind.Regular,
            commercial,
            DateOnly.FromDateTime(DateTime.UtcNow));
        var respondedAt = DateTime.UtcNow;

        var spend = await _tokens.TrySpendAsync(
            billingCompanyId,
            TokenSpendReason.AcceptCandidate,
            vacancyId: null,
            actorUserId: employerUserId,
            branchCompanyId: initiative.BranchCompanyId ?? initiative.RootCompanyId,
            note: $"AcceptInitiative:{initiativeId}",
            onSuccessBeforeCommit: async ct =>
            {
                initiative.Status = WorkInitiativeStatus.Accepted;
                initiative.RespondedAtUtc = respondedAt;
                _db.WorkInitiativeAcceptances.Add(new WorkInitiativeAcceptance
                {
                    InitiativeId = initiativeId,
                    BillingCompanyId = billingCompanyId,
                    AcceptCostTokens = cost,
                    AcceptedAtUtc = respondedAt
                });
                await _db.SaveChangesAsync(ct);
            },
            costOverrides: new Dictionary<TokenSpendReason, decimal>
            {
                [TokenSpendReason.AcceptCandidate] = cost
            },
            cancellationToken: cancellationToken);

        if (!spend.Succeeded)
        {
            return (false, "insufficient_tokens", spend.Balance);
        }

        _logger.LogInformation("Initiative {Id} accepted by {User}", initiativeId, employerUserId);
        return (true, null, spend.Balance);
    }

    public async Task DeclineAsync(
        Guid employerUserId,
        Guid initiativeId,
        bool keepOpen,
        int? reducedMaxPerWeek,
        bool closeTeam,
        CancellationToken cancellationToken = default)
    {
        var initiative = await _db.WorkInitiatives
            .Include(i => i.InitiativeTeam)
            .FirstAsync(i => i.Id == initiativeId, cancellationToken);
        initiative.Status = WorkInitiativeStatus.Declined;
        initiative.RespondedAtUtc = DateTime.UtcNow;

        if (closeTeam && initiative.InitiativeTeam is not null)
        {
            initiative.InitiativeTeam.IsOpen = false;
        }

        if (reducedMaxPerWeek is int max && initiative.InitiativeTeam is not null)
        {
            initiative.InitiativeTeam.MaxPerWeek = Math.Clamp(max, 1, 50);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Initiative {Id} declined by {User}, keepOpen={KeepOpen}", initiativeId, employerUserId, keepOpen);
    }

    public async Task MarkViewedAsync(Guid employerUserId, Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var initiative = await _db.WorkInitiatives.FirstAsync(i => i.Id == initiativeId, cancellationToken);
        if (initiative.Status == WorkInitiativeStatus.Pending)
        {
            initiative.Status = WorkInitiativeStatus.Viewed;
            initiative.EmployerViewedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task WithdrawAsync(Guid candidateUserId, Guid initiativeId, CancellationToken cancellationToken = default)
    {
        var initiative = await _db.WorkInitiatives.FirstAsync(
            i => i.Id == initiativeId && i.CandidateUserId == candidateUserId,
            cancellationToken);
        initiative.Status = WorkInitiativeStatus.Withdrawn;
        initiative.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> AutoDeclineOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.WorkInitiatives
            .Where(i => (i.Status == WorkInitiativeStatus.Pending || i.Status == WorkInitiativeStatus.Viewed)
                        && i.ResponseDeadlineUtc < now)
            .ToListAsync(cancellationToken);

        foreach (var item in overdue)
        {
            item.Status = WorkInitiativeStatus.AutoDeclined;
            item.RespondedAtUtc = now;
            item.CountsTowardWeeklyLimit = false;
        }

        if (overdue.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return overdue.Count;
    }

    private async Task UpsertDraftAsync(
        Guid candidateUserId,
        SendWorkInitiativeRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _db.WorkInitiativeDrafts.FirstOrDefaultAsync(
            d => d.CandidateUserId == candidateUserId
                 && d.RootCompanyId == request.RootCompanyId
                 && d.InitiativeTeamId == request.InitiativeTeamId,
            cancellationToken);

        if (existing is null)
        {
            existing = new WorkInitiativeDraft
            {
                Id = Guid.NewGuid(),
                CandidateUserId = candidateUserId,
                RootCompanyId = request.RootCompanyId,
                BranchCompanyId = request.BranchCompanyId,
                InitiativeTeamId = request.InitiativeTeamId
            };
            _db.WorkInitiativeDrafts.Add(existing);
        }

        existing.Motivation = request.Motivation.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ConfirmResendTeamAsync(Guid rootCompanyId, Guid teamId, CancellationToken cancellationToken)
    {
        var team = await _db.InitiativeTeams.FirstAsync(
            t => t.Id == teamId && t.RootCompanyId == rootCompanyId,
            cancellationToken);
        await IssueEmailCodeAsync(team.ContactEmail, hash => team.ContactEmailConfirmationCodeHash = hash,
            exp => team.ContactEmailConfirmationExpiresAtUtc = exp);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task IssueEmailCodeAsync(string email, Action<string?> setHash, Action<DateTime?> setExpiry)
    {
        var code = VerificationCodes.CreateNumericCode(6);
        setHash(VerificationCodes.Hash(code));
        setExpiry(DateTime.UtcNow.Add(EmailCodeTtl));
        var body = $"Je bevestigingscode is {code}. Deze code is 15 minuten geldig.";
        var mail = new ComposedEmail("initiative-email-confirm", "initiative", "Bevestig je e-mailadres", $"<p>{body}</p>");
        await _mailer.SendAsync(mail, email.Trim(), cancellationToken: CancellationToken.None);
    }

    private static bool VerifyCode(string? hash, DateTime? expires, string code)
    {
        if (hash is null || expires is null || expires < DateTime.UtcNow)
        {
            return false;
        }

        return VerificationCodes.MatchesHash(hash, code);
    }

    private async Task<Guid> ResolveRootCompanyIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.AsNoTracking().FirstAsync(c => c.Id == companyId, cancellationToken);
        return company.ParentCompanyId ?? company.Id;
    }

    private static IReadOnlyList<string> ParsePhotoUrls(string? json)
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

    private static string? ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : address;
    }
}
