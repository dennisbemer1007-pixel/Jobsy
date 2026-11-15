using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.WerkgeverApk;

public interface IWerkgeverApkManagerPortalService
{
    Task<(Guid? CompanyId, WerkgeverApkErrorDto? Error, int Status)> ResolveManagerCompanyAsync(
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<(IReadOnlyList<WerkgeverApkTeamDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListTeamsAsync(
        ClaimsPrincipal user,
        CancellationToken ct = default);

    Task<(WerkgeverApkTeamDto? Ok, WerkgeverApkErrorDto? Error, int Status)> CreateTeamAsync(
        ClaimsPrincipal user,
        WerkgeverApkTeamCreateRequest request,
        CancellationToken ct = default);

    Task<(WerkgeverApkTeamDto? Ok, WerkgeverApkErrorDto? Error, int Status)> RenameTeamAsync(
        ClaimsPrincipal user,
        Guid teamId,
        WerkgeverApkTeamCreateRequest request,
        CancellationToken ct = default);

    Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> DeleteTeamAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default);

    Task<(IReadOnlyList<WerkgeverApkEmployeeCodeRowDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> GenerateCodesAsync(
        ClaimsPrincipal user,
        Guid teamId,
        WerkgeverApkGenerateCodesRequest request,
        CancellationToken ct = default);

    Task<(IReadOnlyList<WerkgeverApkEmployeeCodeRowDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListCodesAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default);

    Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> RevokeCodeAsync(
        ClaimsPrincipal user,
        Guid codeId,
        CancellationToken ct = default);

    Task<(WerkgeverApkTeamOverviewDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetTeamOverviewAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default);

    Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> SetCodeLabelAsync(
        ClaimsPrincipal user,
        Guid codeId,
        WerkgeverApkUpdateEmployeeLabelRequest request,
        CancellationToken ct = default);

    Task<(IReadOnlyList<WerkgeverApkManagerQuestionDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListManagerQuestionsAsync(
        CancellationToken ct = default);

    Task<(WerkgeverApkManagerSheetDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetManagerSheetAsync(
        ClaimsPrincipal user,
        Guid codeId,
        CancellationToken ct = default);

    Task<(WerkgeverApkManagerSheetSubmitResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SaveManagerAnswerAsync(
        ClaimsPrincipal user,
        Guid codeId,
        WerkgeverApkManagerAnswerRequest request,
        CancellationToken ct = default);

    Task<(WerkgeverApkGroupInsightsDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetGroupInsightsAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default);
}

public sealed class WerkgeverApkManagerPortalService : IWerkgeverApkManagerPortalService
{
    private static readonly SchoolLevel Level = SchoolLevel.Mbo3;

    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IWerkgeverApkEmployeeCodeService _codes;
    private readonly IFeatureFlags _features;
    private readonly TimeProvider _clock;

    public WerkgeverApkManagerPortalService(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IWerkgeverApkEmployeeCodeService codes,
        IFeatureFlags features,
        TimeProvider? clock = null)
    {
        _db = db;
        _companyAuth = companyAuth;
        _codes = codes;
        _features = features;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<(Guid? CompanyId, WerkgeverApkErrorDto? Error, int Status)> ResolveManagerCompanyAsync(
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        return (gate.CompanyId, null, 200);
    }

    public async Task<(IReadOnlyList<WerkgeverApkTeamDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListTeamsAsync(
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var teams = await _db.WerkgeverApkTeams.AsNoTracking()
            .Where(t => t.CompanyId == gate.CompanyId)
            .OrderBy(t => t.Name)
            .Select(t => new WerkgeverApkTeamDto(
                t.Id,
                t.CompanyId,
                t.Name,
                t.Codes.Count,
                t.CreatedAtUtc))
            .ToListAsync(ct);
        return (teams, null, 200);
    }

    public async Task<(WerkgeverApkTeamDto? Ok, WerkgeverApkErrorDto? Error, int Status)> CreateTeamAsync(
        ClaimsPrincipal user,
        WerkgeverApkTeamCreateRequest request,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var name = (request.Name ?? "").Trim();
        if (name.Length is < 1 or > 80)
        {
            return (null, new WerkgeverApkErrorDto("invalid_name", "Vul een teamnaam in."), 400);
        }

        if (!Guid.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            && !Guid.TryParse(user.FindFirst("sub")?.Value, out userId))
        {
            return (null, new WerkgeverApkErrorDto("forbidden", "Geen toegang."), 403);
        }

        var entity = new WerkgeverApkTeam
        {
            Id = Guid.NewGuid(),
            CompanyId = gate.CompanyId,
            Name = name,
            CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
            CreatedByUserId = userId
        };
        _db.WerkgeverApkTeams.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (new WerkgeverApkTeamDto(entity.Id, entity.CompanyId, entity.Name, 0, entity.CreatedAtUtc), null, 201);
    }

    public async Task<(WerkgeverApkTeamDto? Ok, WerkgeverApkErrorDto? Error, int Status)> RenameTeamAsync(
        ClaimsPrincipal user,
        Guid teamId,
        WerkgeverApkTeamCreateRequest request,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams
            .Include(t => t.Codes)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        var name = (request.Name ?? "").Trim();
        if (name.Length is < 1 or > 80)
        {
            return (null, new WerkgeverApkErrorDto("invalid_name", "Vul een teamnaam in."), 400);
        }

        team.Name = name;
        await _db.SaveChangesAsync(ct);
        return (new WerkgeverApkTeamDto(team.Id, team.CompanyId, team.Name, team.Codes.Count, team.CreatedAtUtc), null, 200);
    }

    public async Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> DeleteTeamAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (false, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (false, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        _db.WerkgeverApkTeams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return (true, null, 200);
    }

    public async Task<(IReadOnlyList<WerkgeverApkEmployeeCodeRowDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> GenerateCodesAsync(
        ClaimsPrincipal user,
        Guid teamId,
        WerkgeverApkGenerateCodesRequest request,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        await _codes.GenerateAsync(request.Count, team, ct);
        return await ListCodesAsync(user, teamId, ct);
    }

    public async Task<(IReadOnlyList<WerkgeverApkEmployeeCodeRowDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListCodesAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        var rows = await _db.WerkgeverApkEmployeeCodes.AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .OrderBy(c => c.Number)
            .Select(c => new
            {
                c.Id,
                c.Number,
                c.CodeProtected,
                c.Label,
                c.Status,
                c.SharedWithManager,
                c.SharedAtUtc,
                c.LastSeenAtUtc
            })
            .ToListAsync(ct);

        var list = rows.Select(c =>
        {
            var plain = _codes.Unprotect(c.CodeProtected);
            var display = plain is not null ? PupilCodeFormat.Display(plain) : $"#{c.Number}";
            return new WerkgeverApkEmployeeCodeRowDto(
                c.Id,
                c.Number,
                display,
                c.Label,
                c.Status,
                c.SharedWithManager,
                c.SharedAtUtc,
                c.LastSeenAtUtc);
        }).ToList();

        return (list, null, 200);
    }

    public async Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> RevokeCodeAsync(
        ClaimsPrincipal user,
        Guid codeId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (false, gate.Error, gate.Status);
        }

        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Team)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code?.Team is null || code.Team.CompanyId != gate.CompanyId)
        {
            return (false, new WerkgeverApkErrorDto("not_found", "Code niet gevonden."), 404);
        }

        code.Status = WerkgeverApkEmployeeCodeStatus.Revoked;
        code.SessionVersion++;
        await _db.SaveChangesAsync(ct);
        return (true, null, 200);
    }

    public async Task<(WerkgeverApkTeamOverviewDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetTeamOverviewAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        var codes = await _db.WerkgeverApkEmployeeCodes.AsNoTracking()
            .Where(c => c.TeamId == teamId && c.Status != WerkgeverApkEmployeeCodeStatus.Revoked)
            .Select(c => new
            {
                c.Id,
                c.Label,
                c.CodeProtected,
                c.Status,
                c.SharedWithManager,
                ProgressJson = c.Progress != null ? c.Progress.ProgressJson : "{}",
                ManagerCompleted = c.ManagerSheet != null && c.ManagerSheet.Completed
            })
            .ToListAsync(ct);

        var rows = codes.Select(c =>
        {
            var plain = _codes.Unprotect(c.CodeProtected);
            var display = plain is not null ? PupilCodeFormat.Display(plain) : "—";
            var state = PupilMboHboProgressState.Parse(c.ProgressJson);
            var completed = WerkgeverApkEntryPaths.AllEmployeeBlocksComplete(state)
                            || c.Status == WerkgeverApkEmployeeCodeStatus.Completed;
            return new WerkgeverApkTeamOverviewCodeDto(
                c.Id,
                c.Label,
                display,
                c.Status,
                completed,
                c.SharedWithManager,
                c.ManagerCompleted);
        }).ToList();

        return (new WerkgeverApkTeamOverviewDto(teamId, team.Name, rows), null, 200);
    }

    public async Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> SetCodeLabelAsync(
        ClaimsPrincipal user,
        Guid codeId,
        WerkgeverApkUpdateEmployeeLabelRequest request,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (false, gate.Error, gate.Status);
        }

        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Team)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code?.Team is null || code.Team.CompanyId != gate.CompanyId)
        {
            return (false, new WerkgeverApkErrorDto("not_found", "Code niet gevonden."), 404);
        }

        var label = request.Label?.Trim();
        if (label?.Length > 40)
        {
            return (false, new WerkgeverApkErrorDto("invalid_label", "Label is te lang."), 400);
        }

        code.Label = string.IsNullOrWhiteSpace(label) ? null : label;
        await _db.SaveChangesAsync(ct);
        return (true, null, 200);
    }

    public Task<(IReadOnlyList<WerkgeverApkManagerQuestionDto>? Ok, WerkgeverApkErrorDto? Error, int Status)> ListManagerQuestionsAsync(
        CancellationToken ct = default)
    {
        var list = WerkgeverApkManagerQuestionCatalog.All
            .Select(q => new WerkgeverApkManagerQuestionDto(
                q.Id,
                q.Label,
                q.Category.ToString(),
                q.Scenario))
            .ToList();
        return Task.FromResult<(IReadOnlyList<WerkgeverApkManagerQuestionDto>?, WerkgeverApkErrorDto?, int)>((list, null, 200));
    }

    public async Task<(WerkgeverApkManagerSheetDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetManagerSheetAsync(
        ClaimsPrincipal user,
        Guid codeId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Team)
            .Include(c => c.ManagerSheet)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code?.Team is null || code.Team.CompanyId != gate.CompanyId)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Code niet gevonden."), 404);
        }

        var sheet = code.ManagerSheet ?? await EnsureManagerSheetAsync(code, ct);
        var answers = WerkgeverApkComparisonEngine.ParseManagerAnswers(sheet.AnswersJson);
        return (new WerkgeverApkManagerSheetDto(code.Id, code.Label, sheet.Completed, answers), null, 200);
    }

    public async Task<(WerkgeverApkManagerSheetSubmitResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SaveManagerAnswerAsync(
        ClaimsPrincipal user,
        Guid codeId,
        WerkgeverApkManagerAnswerRequest request,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        if (!WerkgeverApkManagerQuestionCatalog.All.Any(q => q.Id == request.QuestionId))
        {
            return (null, new WerkgeverApkErrorDto("invalid_question", "Onbekende vraag."), 400);
        }

        if (request.Value is < 1 or > 5)
        {
            return (null, new WerkgeverApkErrorDto("invalid_value", "Kies een waarde van 1 tot 5."), 400);
        }

        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Team)
            .Include(c => c.ManagerSheet)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code?.Team is null || code.Team.CompanyId != gate.CompanyId)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Code niet gevonden."), 404);
        }

        var sheet = code.ManagerSheet ?? await EnsureManagerSheetAsync(code, ct);
        var answers = WerkgeverApkComparisonEngine.ParseManagerAnswers(sheet.AnswersJson).ToDictionary(
            kv => kv.Key,
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);
        answers[request.QuestionId] = request.Value;
        sheet.AnswersJson = JsonSerializer.Serialize(answers);
        sheet.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        sheet.Completed = WerkgeverApkManagerQuestionCatalog.All.All(q => answers.ContainsKey(q.Id));
        await _db.SaveChangesAsync(ct);
        return (new WerkgeverApkManagerSheetSubmitResponse(sheet.Completed), null, 200);
    }

    public async Task<(WerkgeverApkGroupInsightsDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetGroupInsightsAsync(
        ClaimsPrincipal user,
        Guid teamId,
        CancellationToken ct = default)
    {
        var gate = await EnsureManagerGateAsync(user, ct);
        if (gate.Error is not null)
        {
            return (null, gate.Error, gate.Status);
        }

        var team = await _db.WerkgeverApkTeams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId && t.CompanyId == gate.CompanyId, ct);
        if (team is null)
        {
            return (null, new WerkgeverApkErrorDto("not_found", "Team niet gevonden."), 404);
        }

        var rows = await _db.WerkgeverApkEmployeeCodes.AsNoTracking()
            .Where(c => c.TeamId == teamId
                        && c.SharedWithManager
                        && c.Status == WerkgeverApkEmployeeCodeStatus.Completed)
            .Select(c => new
            {
                c.Progress!.ProgressJson,
                c.ManagerSheet!.AnswersJson
            })
            .ToListAsync(ct);

        var employeeStates = rows
            .Select(r => PupilMboHboProgressState.Parse(r.ProgressJson))
            .ToList();
        var managerSheets = rows
            .Select(r => WerkgeverApkComparisonEngine.ParseManagerAnswers(r.AnswersJson))
            .ToList();

        var insights = WerkgeverApkGroupInsights.Aggregate(employeeStates, managerSheets, Level);
        return (insights, null, 200);
    }

    private async Task<WerkgeverApkManagerSheet> EnsureManagerSheetAsync(
        WerkgeverApkEmployeeCode code,
        CancellationToken ct)
    {
        if (code.ManagerSheet is not null)
        {
            return code.ManagerSheet;
        }

        var sheet = new WerkgeverApkManagerSheet
        {
            EmployeeCodeId = code.Id,
            AnswersJson = "{}",
            Completed = false,
            UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime
        };
        _db.WerkgeverApkManagerSheets.Add(sheet);
        code.ManagerSheet = sheet;
        await _db.SaveChangesAsync(ct);
        return sheet;
    }

    private async Task<ManagerGate> EnsureManagerGateAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole(JobsyRoles.EnterpriseManager) && !user.IsInRole(JobsyRoles.Admin))
        {
            return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("forbidden", "Alleen voor enterprise managers."), 403);
        }

        if (!await WerkgeverApkFeatureGate.IsGlobalEnabledAsync(_features, ct))
        {
            return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("feature_disabled", "Loopbaan-APK staat niet aan."), 404);
        }

        var companyIdClaim = user.FindFirst("company_id")?.Value
                             ?? user.FindFirst("CompanyId")?.Value;
        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(user, ct);
            if (accessible is null || accessible.Count == 0)
            {
                return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("forbidden", "Geen organisatie gevonden."), 403);
            }

            var orgId = await _db.Companies.AsNoTracking()
                .Where(c => accessible.Contains(c.Id) && c.ParentCompanyId == null)
                .OrderBy(c => c.Name)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);
            if (orgId == Guid.Empty)
            {
                return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("forbidden", "Geen organisatie gevonden."), 403);
            }

            companyId = orgId;
        }

        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(user, companyId, ct);
        }
        catch (Core.Exceptions.ForbiddenCompanyAccessException)
        {
            return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("forbidden", "Geen toegang."), 403);
        }

        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company is null || !WerkgeverApkFeatureGate.IsCompanyEnabled(company))
        {
            return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("feature_disabled", "Loopbaan-APK staat niet aan voor deze organisatie."), 404);
        }

        if (company.ParentCompanyId is not null)
        {
            return new ManagerGate(Guid.Empty, new WerkgeverApkErrorDto("invalid_company", "Loopbaan-APK hoort bij de organisatie."), 400);
        }

        return new ManagerGate(companyId, null, 200);
    }

    private sealed record ManagerGate(Guid CompanyId, WerkgeverApkErrorDto? Error, int Status);
}