using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.WerkgeverApk;

public interface IWerkgeverApkEmployeePortalService
{
    Task<(WerkgeverApkEmployeeLoginResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> LoginAsync(
        WerkgeverApkEmployeeLoginRequest request,
        CancellationToken ct = default);

    ClaimsPrincipal CreatePrincipal(WerkgeverApkEmployeeLoginResponse login);

    Task<(WerkgeverApkEmployeeProgressDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetProgressAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default);

    Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> MarkWelcomeSeenAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default);

    Task<(WerkgeverApkEmployeeQuestionDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetQuestionAsync(
        ClaimsPrincipal employee,
        PupilMboHboBlock block,
        string? itemId,
        CancellationToken ct = default);

    Task<(WerkgeverApkEmployeeAnswerResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SaveAnswerAsync(
        ClaimsPrincipal employee,
        PupilMboHboBlock block,
        string itemId,
        int value,
        CancellationToken ct = default);

    Task<(WerkgeverApkShareWithManagerResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SetShareAsync(
        ClaimsPrincipal employee,
        bool share,
        CancellationToken ct = default);

    Task<(WerkgeverApkEmployeeComparisonResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> GetComparisonAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default);

    Task<(WerkgeverApkSharePreviewDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetSharePreviewAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default);
}

public sealed class WerkgeverApkEmployeePortalService : IWerkgeverApkEmployeePortalService
{
    public const string InvalidCode = "invalid_code";
    public const string SessionInvalid = "session_invalid";
    public const string FeatureOff = "feature_disabled";

    private static readonly SchoolLevel Level = SchoolLevel.Mbo3;

    private readonly JobsyDbContext _db;
    private readonly IWerkgeverApkEmployeeCodeService _codes;
    private readonly IFeatureFlags _features;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public WerkgeverApkEmployeePortalService(
        JobsyDbContext db,
        IWerkgeverApkEmployeeCodeService codes,
        IFeatureFlags features,
        IMemoryCache cache,
        TimeProvider? clock = null)
    {
        _db = db;
        _codes = codes;
        _features = features;
        _cache = cache;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<(WerkgeverApkEmployeeLoginResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> LoginAsync(
        WerkgeverApkEmployeeLoginRequest request,
        CancellationToken ct = default)
    {
        if (!await WerkgeverApkFeatureGate.IsGlobalEnabledAsync(_features, ct))
        {
            return (null, new WerkgeverApkErrorDto(FeatureOff, "Loopbaan-APK staat niet aan."), 404);
        }

        if (!PupilCodeFormat.TryNormalize(request.Code, out var normalized))
        {
            return (null, new WerkgeverApkErrorDto(InvalidCode, "Controleer je code en probeer het opnieuw."), 401);
        }

        var hash = _codes.LookupHash(normalized);
        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Progress)
            .Include(c => c.ManagerSheet)
            .Include(c => c.Team)!.ThenInclude(t => t!.Company)
            .FirstOrDefaultAsync(c => c.CodeLookupHash == hash, ct);
        if (code?.Team is null || code.Team.Company is null)
        {
            return (null, new WerkgeverApkErrorDto(InvalidCode, "Controleer je code en probeer het opnieuw."), 401);
        }

        var team = code.Team;
        if (!WerkgeverApkFeatureGate.IsCompanyEnabled(team.Company))
        {
            return (null, new WerkgeverApkErrorDto(InvalidCode, "Controleer je code en probeer het opnieuw."), 401);
        }
        if (code is null || code.Status == WerkgeverApkEmployeeCodeStatus.Revoked)
        {
            return (null, new WerkgeverApkErrorDto(InvalidCode, "Controleer je code en probeer het opnieuw."), 401);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        code.SessionVersion++;
        code.LastSeenAtUtc = now;
        if (code.Status == WerkgeverApkEmployeeCodeStatus.NotStarted)
        {
            code.Status = WerkgeverApkEmployeeCodeStatus.InProgress;
        }

        var progress = await EnsureProgressAsync(code, ct);
        var state = LoadState(progress);
        var completed = IsEmployeeCompleted(code, state);
        if (completed && code.Status != WerkgeverApkEmployeeCodeStatus.Completed)
        {
            code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
        }

        await _db.SaveChangesAsync(ct);
        _cache.Remove($"werkgever-apk-session:{code.Id:D}");

        var welcomeSeen = progress.WelcomeSeen;
        var redirect = WerkgeverApkEntryPaths.AfterLogin(welcomeSeen, state, completed, code.SharedWithManager);
        var display = PupilCodeFormat.Display(normalized);
        var response = new WerkgeverApkEmployeeLoginResponse(
            redirect,
            code.Id,
            team.Id,
            team.CompanyId,
            team.Name,
            team.Company.Name,
            display,
            code.Status,
            code.SessionVersion,
            welcomeSeen);
        return (response, null, 200);
    }

    public ClaimsPrincipal CreatePrincipal(WerkgeverApkEmployeeLoginResponse login)
    {
        var iat = _clock.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(WerkgeverApkClaimTypes.EmployeeCodeId, login.EmployeeCodeId.ToString("D")),
            new(WerkgeverApkClaimTypes.TeamId, login.TeamId.ToString("D")),
            new(WerkgeverApkClaimTypes.CompanyId, login.CompanyId.ToString("D")),
            new(WerkgeverApkClaimTypes.SessionVersion, login.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(WerkgeverApkClaimTypes.IssuedAt, iat),
            new(WerkgeverApkClaimTypes.CodeDisplay, login.CodeDisplay),
            new(WerkgeverApkClaimTypes.TeamName, login.TeamName),
            new(WerkgeverApkClaimTypes.CompanyName, login.CompanyName),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, WerkgeverApkAuthDefaults.Scheme));
    }

    public async Task<(WerkgeverApkEmployeeProgressDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetProgressAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, progress, state) = (ctx.Code!, ctx.Progress!, ctx.State!);
        var catalog = PupilMboHboCatalog.Default;
        var total = WerkgeverApkEntryPaths.EmployeeBlocks.Sum(b => catalog.QuestionCount(b, Level));
        var answered = WerkgeverApkEntryPaths.EmployeeBlocks.Sum(b => state.AnsweredInBlock(b, catalog, Level));
        var dto = new WerkgeverApkEmployeeProgressDto(
            code.Id,
            employee.FindFirst(WerkgeverApkClaimTypes.CodeDisplay)?.Value ?? "",
            employee.FindFirst(WerkgeverApkClaimTypes.TeamName)?.Value ?? "",
            code.Status,
            progress.WelcomeSeen,
            IsEmployeeCompleted(code, state),
            progress.ProgressJson,
            answered,
            total);
        return (dto, null, 200);
    }

    public async Task<(bool Ok, WerkgeverApkErrorDto? Error, int Status)> MarkWelcomeSeenAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (false, ctx.Error, ctx.Status);
        }

        var progress = ctx.Progress!;
        progress.WelcomeSeen = true;
        progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(ct);
        return (true, null, 200);
    }

    public async Task<(WerkgeverApkEmployeeQuestionDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetQuestionAsync(
        ClaimsPrincipal employee,
        PupilMboHboBlock block,
        string? itemId,
        CancellationToken ct = default)
    {
        if (!WerkgeverApkEntryPaths.EmployeeBlocks.Contains(block))
        {
            return (null, new WerkgeverApkErrorDto("invalid_block", "Onbekende test."), 400);
        }

        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var state = ctx.State!;
        var catalog = PupilMboHboCatalog.Default;
        var questions = catalog.QuestionsFor(block, Level);
        if (questions.Count == 0)
        {
            return (null, new WerkgeverApkErrorDto("invalid_block", "Onbekende test."), 400);
        }

        var q = ResolveQuestion(questions, state, itemId);
        if (q is null)
        {
            return (null, new WerkgeverApkErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        var list = questions.ToList();
        var index = list.FindIndex(x => x.Id == q.Id) + 1;
        var qIndex = list.FindIndex(x => x.Id == q.Id);
        string? previousItemId = qIndex > 0 ? list[qIndex - 1].Id : null;
        int? val = state.Answers.TryGetValue(q.Id, out var v) ? v : null;

        return (new WerkgeverApkEmployeeQuestionDto(
            block,
            PupilMboHboProgressState.BlockKey(block),
            q.Id,
            index,
            questions.Count,
            q.Dimension,
            q.Question,
            q.Scenario,
            PupilMboHboScaleLabels.ForBlock(block),
            val,
            previousItemId), null, 200);
    }

    public async Task<(WerkgeverApkEmployeeAnswerResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SaveAnswerAsync(
        ClaimsPrincipal employee,
        PupilMboHboBlock block,
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        if (!WerkgeverApkEntryPaths.EmployeeBlocks.Contains(block))
        {
            return (null, new WerkgeverApkErrorDto("invalid_block", "Onbekende test."), 400);
        }

        if (!LikertAnswerJson.IsValidAnswer(value))
        {
            return (null, new WerkgeverApkErrorDto("invalid_value", "Kies een antwoord."), 400);
        }

        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, progress, state) = (ctx.Code!, ctx.Progress!, ctx.State!);
        var catalog = PupilMboHboCatalog.Default;
        var questions = catalog.QuestionsFor(block, Level);
        if (questions.All(q => q.Id != itemId))
        {
            return (null, new WerkgeverApkErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        state.Answers[itemId] = value;
        var blockComplete = state.BlockFullyAnswered(block, catalog, Level);
        if (blockComplete)
        {
            var key = PupilMboHboProgressState.BlockKey(block);
            state.BlocksCompleted[key] = _clock.GetUtcNow().UtcDateTime.ToString("O");
        }

        progress.ProgressJson = state.ToJson();
        progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        if (IsEmployeeCompleted(code, state))
        {
            code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
        }
        else if (code.Status == WerkgeverApkEmployeeCodeStatus.NotStarted)
        {
            code.Status = WerkgeverApkEmployeeCodeStatus.InProgress;
        }

        await _db.SaveChangesAsync(ct);

        string? nextItemId = null;
        string? nextPath = null;
        var list = questions.ToList();
        var idx = list.FindIndex(q => q.Id == itemId);
        if (idx >= 0 && idx < list.Count - 1)
        {
            nextItemId = list[idx + 1].Id;
        }
        else if (blockComplete)
        {
            foreach (var nextBlock in WerkgeverApkEntryPaths.EmployeeBlocks)
            {
                if (!state.IsBlockComplete(nextBlock))
                {
                    nextPath = WerkgeverApkEntryPaths.Test(nextBlock);
                    break;
                }
            }

            if (nextPath is null && IsEmployeeCompleted(code, state))
            {
                nextPath = WerkgeverApkEntryPaths.Delen;
            }
        }

        return (new WerkgeverApkEmployeeAnswerResponse(nextItemId, nextPath, blockComplete), null, 200);
    }

    public async Task<(WerkgeverApkShareWithManagerResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SetShareAsync(
        ClaimsPrincipal employee,
        bool share,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var code = ctx.Code!;
        if (!IsEmployeeCompleted(code, ctx.State!))
        {
            return (null, new WerkgeverApkErrorDto("not_complete", "Maak eerst de tests af."), 400);
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        if (share)
        {
            code.SharedWithManager = true;
            code.SharedAtUtc = now;
            code.RevokedAtUtc = null;
        }
        else
        {
            code.SharedWithManager = false;
            code.RevokedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
        return (new WerkgeverApkShareWithManagerResponse(code.SharedWithManager, code.SharedAtUtc), null, 200);
    }

    public async Task<(WerkgeverApkEmployeeComparisonResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> GetComparisonAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var code = ctx.Code!;
        if (!code.SharedWithManager)
        {
            return (null, new WerkgeverApkErrorDto("not_shared", "Je hebt nog niet gedeeld met je leidinggevende."), 403);
        }

        var sheet = await _db.WerkgeverApkManagerSheets.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeCodeId == code.Id, ct);
        if (sheet is null || !sheet.Completed)
        {
            return (null, new WerkgeverApkErrorDto("manager_pending", "Je leidinggevende is nog bezig met het herkenningsformulier."), 409);
        }

        var managerAnswers = WerkgeverApkComparisonEngine.ParseManagerAnswers(sheet.AnswersJson);
        var comparison = WerkgeverApkComparisonEngine.BuildComparison(ctx.State!, managerAnswers, Level);
        return (new WerkgeverApkEmployeeComparisonResponse(comparison), null, 200);
    }

    public async Task<(WerkgeverApkSharePreviewDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetSharePreviewAsync(
        ClaimsPrincipal employee,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var code = ctx.Code!;
        var completed = IsEmployeeCompleted(code, ctx.State!);
        if (!completed)
        {
            return (null, new WerkgeverApkErrorDto("not_complete", "Maak eerst de tests af."), 400);
        }

        var catalog = PupilMboHboCatalog.Default;
        var bullets = new List<string>
        {
            "Een kort overzicht van je interesses (geen losse antwoorden).",
            "Je sterkste kanten uit de competentietest.",
            "Wat je belangrijk vindt in je werk."
        };
        var interesse = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Interesse, ctx.State!, Level, catalog);
        if (!string.IsNullOrWhiteSpace(interesse.Summary))
        {
            bullets.Add(interesse.Summary);
        }

        var dto = new WerkgeverApkSharePreviewDto(
            code.SharedWithManager,
            completed,
            "je leidinggevende",
            bullets);
        return (dto, null, 200);
    }

    private async Task<WerkgeverApkEmployeeProgress> EnsureProgressAsync(
        WerkgeverApkEmployeeCode code,
        CancellationToken ct)
    {
        if (code.Progress is not null)
        {
            return code.Progress;
        }

        var row = new WerkgeverApkEmployeeProgress
        {
            EmployeeCodeId = code.Id,
            ProgressJson = "{}",
            WelcomeSeen = false,
            UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime
        };
        _db.WerkgeverApkEmployeeProgress.Add(row);
        code.Progress = row;
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private static PupilMboHboProgressState LoadState(WerkgeverApkEmployeeProgress progress)
        => PupilMboHboProgressState.Parse(progress.ProgressJson);

    private static bool IsEmployeeCompleted(WerkgeverApkEmployeeCode code, PupilMboHboProgressState state)
        => code.Status == WerkgeverApkEmployeeCodeStatus.Completed
           || WerkgeverApkEntryPaths.AllEmployeeBlocksComplete(state);

    private static PupilMboHboQuestion? ResolveQuestion(
        IReadOnlyList<PupilMboHboQuestion> questions,
        PupilMboHboProgressState state,
        string? itemId)
    {
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return questions.FirstOrDefault(q => q.Id == itemId);
        }

        foreach (var q in questions)
        {
            if (!state.Answers.ContainsKey(q.Id))
            {
                return q;
            }
        }

        return questions.Count > 0 ? questions[0] : null;
    }

    private async Task<EmployeeContext> ResolveEmployeeAsync(ClaimsPrincipal employee, CancellationToken ct)
    {
        if (!Guid.TryParse(employee.FindFirst(WerkgeverApkClaimTypes.EmployeeCodeId)?.Value, out var codeId))
        {
            return new EmployeeContext(null, null, null, new WerkgeverApkErrorDto(SessionInvalid, "Sessie verlopen."), 401);
        }

        var code = await _db.WerkgeverApkEmployeeCodes
            .Include(c => c.Progress)
            .Include(c => c.Team)!.ThenInclude(t => t!.Company)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code is null || code.Status == WerkgeverApkEmployeeCodeStatus.Revoked)
        {
            return new EmployeeContext(null, null, null, new WerkgeverApkErrorDto(SessionInvalid, "Sessie verlopen."), 401);
        }

        var progress = code.Progress ?? await EnsureProgressAsync(code, ct);
        var state = LoadState(progress);
        return new EmployeeContext(code, progress, state, null, 200);
    }

    private sealed record EmployeeContext(
        WerkgeverApkEmployeeCode? Code,
        WerkgeverApkEmployeeProgress? Progress,
        PupilMboHboProgressState? State,
        WerkgeverApkErrorDto? Error,
        int Status);
}
