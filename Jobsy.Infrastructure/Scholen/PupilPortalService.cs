using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Scholen;

public interface IPupilPortalService
{
    Task<IReadOnlyList<PupilSchoolOptionDto>> ListSchoolsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PupilClassOptionDto>?> ListClassesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<(PupilLoginResponse? Ok, PupilErrorDto? Error, int StatusCode)> LoginAsync(
        PupilLoginRequest request,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(PupilProgressStateDto? Ok, PupilErrorDto? Error, int StatusCode)> GetProgressAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default);

    Task<(PupilAnswerResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveAnswerAsync(
        ClaimsPrincipal pupil,
        string itemId,
        int value,
        CancellationToken cancellationToken = default);

    Task<(PupilChipsResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveChipsAsync(
        ClaimsPrincipal pupil,
        PupilChipsRequest request,
        CancellationToken cancellationToken = default);

    Task<(PupilResultPageDto? Ok, PupilErrorDto? Error, int StatusCode)> GetResultAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default);

    Task<(PupilDreamJobResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveDreamJobAsync(
        ClaimsPrincipal pupil,
        string? key,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, PupilErrorDto? Error, int StatusCode)> BuildPdfAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, PupilErrorDto? Error, int StatusCode)> ClearLoginPauseAsync(
        ClaimsPrincipal staff,
        Guid classId,
        CancellationToken cancellationToken = default);

    ClaimsPrincipal CreatePrincipal(PupilLoginResponse login);
}

public sealed class PupilPortalService : IPupilPortalService
{
    public const string GenericCodeError = "invalid_code";
    public const string WindowClosedError = "window_closed";
    public const string LoginPausedError = "login_paused";
    public const string CooldownError = "cooldown";
    public const string SchoolInactiveError = "school_inactive";
    public const string SessionInvalidError = "session_invalid";

    private readonly JobsyDbContext _db;
    private readonly IPupilCodeService _codes;
    private readonly IPupilLoginProtection _protection;
    private readonly IPupilQuestionBank _bank;
    private readonly IPupilResultBuilder _results;
    private readonly ISchoolScopeService _scope;
    private readonly IMemoryCache _cache;
    private readonly IPupilStoryRenderer _story;
    private readonly IPupilReportPdfService _pdf;
    private readonly TimeProvider _clock;
    private readonly ILogger<PupilPortalService> _logger;

    public PupilPortalService(
        JobsyDbContext db,
        IPupilCodeService codes,
        IPupilLoginProtection protection,
        IPupilQuestionBank bank,
        IPupilResultBuilder results,
        ISchoolScopeService scope,
        IMemoryCache cache,
        IPupilStoryRenderer story,
        IPupilReportPdfService pdf,
        ILogger<PupilPortalService> logger,
        TimeProvider? clock = null)
    {
        _db = db;
        _codes = codes;
        _protection = protection;
        _bank = bank;
        _results = results;
        _scope = scope;
        _cache = cache;
        _story = story;
        _pdf = pdf;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<PupilSchoolOptionDto>> ListSchoolsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var currentYear = SchoolYear.Current(DateOnly.FromDateTime(now));

        var schools = await _db.Schools.AsNoTracking()
            .Where(s => s.IsActive && s.ProcessorAgreementSignedOn != null)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.City })
            .ToListAsync(cancellationToken);

        var classMeta = await _db.SchoolClasses.AsNoTracking()
            .Where(c => schools.Select(s => s.Id).Contains(c.SchoolId))
            .Select(c => new
            {
                c.SchoolId,
                c.TestWindow,
                c.SchoolYearStart,
                HasCompleted = c.PupilCodes.Any(p => p.Status == PupilCodeStatus.Completed)
            })
            .ToListAsync(cancellationToken);

        var result = new List<PupilSchoolOptionDto>();
        foreach (var s in schools)
        {
            var classes = classMeta.Where(c => c.SchoolId == s.Id).ToList();
            var open = classes.Any(c => c.TestWindow == TestWindowState.Open);
            var readOnly = !open && classes.Any(c =>
                c.SchoolYearStart == currentYear
                && c.TestWindow == TestWindowState.Closed
                && c.HasCompleted);
            if (!open && !readOnly)
            {
                continue;
            }

            result.Add(new PupilSchoolOptionDto(s.Id, s.Name, s.City, readOnly && !open));
        }

        return result;
    }

    public async Task<IReadOnlyList<PupilClassOptionDto>?> ListClassesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var school = await _db.Schools.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schoolId && s.IsActive && s.ProcessorAgreementSignedOn != null,
                cancellationToken);
        if (school is null)
        {
            return null;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var currentYear = SchoolYear.Current(DateOnly.FromDateTime(now));

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Level,
                c.Year,
                c.TestWindow,
                c.SchoolYearStart,
                HasCompleted = c.PupilCodes.Any(p => p.Status == PupilCodeStatus.Completed)
            })
            .ToListAsync(cancellationToken);

        var result = new List<PupilClassOptionDto>();
        foreach (var c in classes)
        {
            var open = c.TestWindow == TestWindowState.Open;
            var readOnly = !open
                           && c.SchoolYearStart == currentYear
                           && c.TestWindow == TestWindowState.Closed
                           && c.HasCompleted;
            if (!open && !readOnly)
            {
                continue;
            }

            var label = $"{c.Name} · {FormatLevel(c.Level)} {c.Year}";
            result.Add(new PupilClassOptionDto(c.Id, label, c.Name, c.Level, c.Year, readOnly && !open));
        }

        return result;
    }

    public async Task<(PupilLoginResponse? Ok, PupilErrorDto? Error, int StatusCode)> LoginAsync(
        PupilLoginRequest request,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var partition = _protection.ClientPartition(clientIp, userAgent);
        var now = _clock.GetUtcNow().UtcDateTime;

        if (!_protection.TryAcquireClassPartition(request.ClassId, partition))
        {
            return (null, new PupilErrorDto(CooldownError,
                "Even pauze. Probeer het over een kwartier opnieuw of vraag je leraar."), 429);
        }

        var schoolClass = await _db.SchoolClasses
            .Include(c => c.School)
            .FirstOrDefaultAsync(c => c.Id == request.ClassId, cancellationToken);
        if (schoolClass is null
            || schoolClass.SchoolId != request.SchoolId
            || schoolClass.School is null
            || !schoolClass.School.IsActive
            || schoolClass.School.ProcessorAgreementSignedOn is null)
        {
            _protection.RecordFailure(request.ClassId, partition);
            await MaybePauseClassAsync(request.ClassId, cancellationToken);
            return GenericFail();
        }

        if (schoolClass.LoginPausedUntilUtc is DateTime paused && paused > now)
        {
            return (null, new PupilErrorDto(LoginPausedError,
                "Even pauze. Probeer het over een kwartier opnieuw of vraag je leraar."), 429);
        }

        if (!PupilCodeFormat.TryNormalize(request.Code, out var normalized))
        {
            _protection.RecordFailure(request.ClassId, partition);
            await MaybePauseClassAsync(request.ClassId, cancellationToken);
            return GenericFail();
        }

        var hash = _codes.LookupHash(normalized);
        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .FirstOrDefaultAsync(
                c => c.SchoolClassId == request.ClassId && c.CodeLookupHash == hash,
                cancellationToken);

        if (code is null)
        {
            _protection.RecordFailure(request.ClassId, partition);
            await MaybePauseClassAsync(request.ClassId, cancellationToken);
            return GenericFail();
        }

        if (code.LockedUntilUtc is DateTime locked && locked > now)
        {
            return (null, new PupilErrorDto(CooldownError,
                "Even pauze. Probeer het over een kwartier opnieuw of vraag je leraar."), 429);
        }

        await EnsureResultAsync(code, cancellationToken);

        var completed = code.Status == PupilCodeStatus.Completed
                        || code.Progress?.CompletedAtUtc is not null
                        || code.Result is not null;
        if (schoolClass.TestWindow != TestWindowState.Open && !completed)
        {
            return (null, new PupilErrorDto(WindowClosedError,
                "Het testvenster van je klas is dicht. Je leraar zet het weer open."), 409);
        }

        // Success path
        _protection.ClearFailures(request.ClassId, partition);
        var successCount = _protection.RecordCodeSuccess(code.Id);
        if (successCount > PupilLoginProtection.CodeSuccessLimit)
        {
            code.LockedUntilUtc = now.Add(PupilLoginProtection.CodeLockDuration);
        }

        code.SessionVersion++;
        code.LastSeenAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove($"pupil-sv:{code.Id:D}");
        _cache.Remove($"pupil-session:{code.Id:D}");

        var total = _bank.AllItems.Count;
        var currentIndex = code.Progress?.CurrentIndex ?? 0;
        if (code.Progress is not null && !string.IsNullOrWhiteSpace(code.Progress.AnswersJson))
        {
            currentIndex = FirstUnansweredIndex(code.Progress.AnswersJson, total);
            if (code.Progress.CurrentIndex != currentIndex && !completed)
            {
                code.Progress.CurrentIndex = currentIndex;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var redirect = completed
            ? "/leerling/dit-ben-jij"
            : code.Status == PupilCodeStatus.NotStarted && (code.Progress is null || currentIndex == 0)
                ? "/leerling/start"
                : "/leerling/reis";

        var display = PupilCodeFormat.Display(normalized);
        var response = new PupilLoginResponse(
            redirect,
            code.Id,
            schoolClass.Id,
            schoolClass.SchoolId,
            schoolClass.Name,
            display,
            code.Status,
            currentIndex,
            total,
            code.SessionVersion);

        return (response, null, 200);
    }

    public ClaimsPrincipal CreatePrincipal(PupilLoginResponse login)
    {
        var iat = _clock.GetUtcNow().ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(PupilClaimTypes.PupilCodeId, login.PupilCodeId.ToString("D")),
            new(PupilClaimTypes.ClassId, login.ClassId.ToString("D")),
            new(PupilClaimTypes.SchoolId, login.SchoolId.ToString("D")),
            new(PupilClaimTypes.SessionVersion, login.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(PupilClaimTypes.IssuedAt, iat),
            new(PupilClaimTypes.ClassLabel, login.ClassLabel),
            new(PupilClaimTypes.CodeDisplay, login.CodeDisplay),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, PupilAuthDefaults.Scheme));
    }

    public async Task<(PupilProgressStateDto? Ok, PupilErrorDto? Error, int StatusCode)> GetProgressAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        var schoolClass = ctx.Class!;
        await EnsureResultAsync(code, cancellationToken);

        var answers = ParseAnswers(code.Progress?.AnswersJson);
        var total = _bank.AllItems.Count;
        var answered = answers.Count;
        var currentIndex = FirstUnansweredIndex(code.Progress?.AnswersJson ?? "{}", total);
        var completed = code.Status == PupilCodeStatus.Completed
                        || code.Progress?.CompletedAtUtc is not null
                        || code.Result is not null;
        var windowOpen = schoolClass.TestWindow == TestWindowState.Open;
        var islandDone = code.Progress?.ChipsSavedAtUtc is not null;
        var needsIsland = !completed && answered >= 30 && !islandDone;
        var item = completed || needsIsland ? null : _bank.GetByGlobalIndex(currentIndex);
        var plates = PupilWorldCatalog.PlatesShed(answered);
        var likes = ParseTagList(code.Progress?.LikesJson);
        var dislikes = ParseTagList(code.Progress?.DislikesJson);

        return (new PupilProgressStateDto(
            code.Id,
            schoolClass.Name,
            pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value ?? "",
            code.Status,
            currentIndex,
            total,
            answered,
            plates,
            answered >= PupilWorldCatalog.TotalTestItems || plates >= PupilWorldCatalog.PlateCount,
            item?.Id,
            needsIsland ? "pauze-eiland" : item?.WorldKey,
            answers,
            windowOpen,
            completed,
            needsIsland,
            islandDone,
            likes,
            dislikes,
            code.Progress?.LikeOtherWord,
            code.Progress?.DislikeOtherWord), null, 200);
    }

    public async Task<(PupilAnswerResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveAnswerAsync(
        ClaimsPrincipal pupil,
        string itemId,
        int value,
        CancellationToken cancellationToken = default)
    {
        if (value is < 1 or > 5)
        {
            return (null, new PupilErrorDto("validation", "Antwoord moet 1–5 zijn."), 400);
        }

        var item = _bank.GetById(itemId);
        if (item is null)
        {
            return (null, new PupilErrorDto("not_found", "Vraag niet gevonden."), 404);
        }

        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        var schoolClass = ctx.Class!;
        var now = _clock.GetUtcNow().UtcDateTime;

        if (code.Status == PupilCodeStatus.Completed || code.Progress?.CompletedAtUtc is not null)
        {
            return (null, new PupilErrorDto("already_completed", "Je reis is al klaar."), 409);
        }

        if (schoolClass.TestWindow != TestWindowState.Open)
        {
            return (null, new PupilErrorDto(WindowClosedError,
                "Je antwoorden zijn bewaard. Je leraar zet de test weer open."), 409);
        }

        var progress = code.Progress;
        if (progress is null)
        {
            progress = new PupilProgress
            {
                PupilCodeId = code.Id,
                AnswersJson = "{}",
                CurrentIndex = 0,
                StartedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.PupilProgresses.Add(progress);
            code.Progress = progress;
        }

        var answers = ParseAnswers(progress.AnswersJson);
        answers[item.Id] = value;
        progress.AnswersJson = JsonSerializer.Serialize(answers);
        progress.UpdatedAtUtc = now;
        code.LastSeenAtUtc = now;
        if (code.Status == PupilCodeStatus.NotStarted)
        {
            code.Status = PupilCodeStatus.InProgress;
        }

        var total = _bank.AllItems.Count;
        var nextIndex = FirstUnansweredIndex(progress.AnswersJson, total);
        progress.CurrentIndex = nextIndex;
        var allAnswered = answers.Count >= total
                        && _bank.AllItems.All(i => answers.ContainsKey(i.Id));
        var islandDone = progress.ChipsSavedAtUtc is not null;
        var needsIsland = !allAnswered && answers.Count >= 30 && !islandDone;
        var resultPending = false;
        var completed = false;

        string? nextId = null;
        string? nextWorld = null;
        // Persist answers first. Never mark Completed before BuildAsync succeeds —
        // otherwise a builder failure leaves the pupil stuck without a PupilResult.
        await _db.SaveChangesAsync(cancellationToken);

        if (allAnswered)
        {
            try
            {
                await _results.BuildAsync(code.Id, cancellationToken);
                await _db.Entry(code).ReloadAsync(cancellationToken);
                if (code.Progress is not null)
                {
                    await _db.Entry(code.Progress).ReloadAsync(cancellationToken);
                }

                await _db.Entry(code).Reference(c => c.Result).LoadAsync(cancellationToken);
                completed = code.Status == PupilCodeStatus.Completed && code.Result is not null;
                if (!completed)
                {
                    resultPending = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pupil result builder failed for {CodeId}", code.Id);
                // Builder may have mutated tracked entities before failing — restore DB state.
                await _db.Entry(code).ReloadAsync(cancellationToken);
                if (code.Progress is not null)
                {
                    await _db.Entry(code.Progress).ReloadAsync(cancellationToken);
                }

                resultPending = true;
                completed = false;
            }
        }
        else if (needsIsland)
        {
            nextWorld = "pauze-eiland";
        }
        else
        {
            var next = _bank.GetByGlobalIndex(nextIndex);
            nextId = next?.Id;
            nextWorld = next?.WorldKey;
        }

        return (new PupilAnswerResponse(
            nextIndex,
            answers.Count,
            PupilWorldCatalog.PlatesShed(answers.Count),
            completed,
            nextId,
            nextWorld,
            needsIsland,
            resultPending), null, 200);
    }

    public async Task<(PupilChipsResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveChipsAsync(
        ClaimsPrincipal pupil,
        PupilChipsRequest request,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        var schoolClass = ctx.Class!;
        var now = _clock.GetUtcNow().UtcDateTime;

        if (code.Status == PupilCodeStatus.Completed || code.Progress?.CompletedAtUtc is not null)
        {
            return (null, new PupilErrorDto("already_completed", "Je reis is al klaar."), 409);
        }

        if (schoolClass.TestWindow != TestWindowState.Open)
        {
            return (null, new PupilErrorDto(WindowClosedError,
                "Je antwoorden zijn bewaard. Je leraar zet de test weer open."), 409);
        }

        var (likes, dislikes, likeOther, dislikeOther, error) = ValidateChips(request);
        if (error is not null)
        {
            return (null, error, 400);
        }

        var progress = code.Progress;
        if (progress is null)
        {
            progress = new PupilProgress
            {
                PupilCodeId = code.Id,
                AnswersJson = "{}",
                CurrentIndex = 0,
                StartedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.PupilProgresses.Add(progress);
            code.Progress = progress;
        }

        progress.LikesJson = JsonSerializer.Serialize(likes);
        progress.DislikesJson = JsonSerializer.Serialize(dislikes);
        progress.LikeOtherWord = likeOther;
        progress.DislikeOtherWord = dislikeOther;
        progress.ChipsSavedAtUtc = now;
        progress.UpdatedAtUtc = now;
        code.LastSeenAtUtc = now;

        var total = _bank.AllItems.Count;
        var nextIndex = FirstUnansweredIndex(progress.AnswersJson, total);
        progress.CurrentIndex = nextIndex;
        await _db.SaveChangesAsync(cancellationToken);

        var next = _bank.GetByGlobalIndex(nextIndex);
        return (new PupilChipsResponse(true, nextIndex, next?.Id, next?.WorldKey), null, 200);
    }

    public async Task<(PupilResultPageDto? Ok, PupilErrorDto? Error, int StatusCode)> GetResultAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        var schoolClass = ctx.Class!;
        await EnsureResultAsync(code, cancellationToken);
        if (code.Status != PupilCodeStatus.Completed || code.Result is null)
        {
            return (null, new PupilErrorDto("not_completed", "Je reis is nog niet klaar."), 409);
        }

        var story = _story.Render(code.Result, code.Progress);
        var dream = _story.RenderDreamRoute(code.Result, code.Progress);
        var likes = ResolveChipLabels(ParseTagList(code.Progress?.LikesJson));
        var dislikes = ResolveChipLabels(ParseTagList(code.Progress?.DislikesJson));
        if (!string.IsNullOrWhiteSpace(code.Progress?.LikeOtherWord))
        {
            likes = likes.Concat([code.Progress!.LikeOtherWord!]).ToList();
        }

        if (!string.IsNullOrWhiteSpace(code.Progress?.DislikeOtherWord))
        {
            dislikes = dislikes.Concat([code.Progress!.DislikeOtherWord!]).ToList();
        }

        var schoolName = await _db.Schools.AsNoTracking()
            .Where(s => s.Id == schoolClass.SchoolId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        return (new PupilResultPageDto(
            ClassLabel: schoolClass.Name,
            CodeDisplay: pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value ?? "",
            SchoolName: schoolName,
            Story: story,
            Likes: likes,
            Dislikes: dislikes,
            DreamJob: dream,
            DreamJobKey: code.Result.DreamJobKey ?? code.Progress?.DreamJobKey), null, 200);
    }

    public async Task<(PupilDreamJobResponse? Ok, PupilErrorDto? Error, int StatusCode)> SaveDreamJobAsync(
        ClaimsPrincipal pupil,
        string? key,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        if (code.Status != PupilCodeStatus.Completed || code.Result is null || code.Progress is null)
        {
            return (null, new PupilErrorDto("not_completed", "Je reis is nog niet klaar."), 409);
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return (null, new PupilErrorDto("invalid_key", "Kies een beroep uit de lijst."), 400);
        }

        var trimmed = key.Trim();
        if (!PupilDreamJobFit.IsKnownJobKey(trimmed))
        {
            return (null, new PupilErrorDto("invalid_key", "Onbekend beroep."), 400);
        }

        var storeKey = string.Equals(trimmed, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase)
            ? PupilDreamJobFit.UndecidedKey
            : DreamJobCatalog.All.First(j =>
                string.Equals(j.Key, trimmed, StringComparison.OrdinalIgnoreCase)).Key;

        code.Progress.DreamJobKey = storeKey;
        code.Progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        code.Result.DreamJobKey = storeKey;

        var fit = PupilDreamJobFit.Evaluate(code.Result, code.Progress, storeKey);
        code.Result.FitSnapshotJson = fit is null ? null : PupilDreamJobFit.SerializeSnapshot(fit.Snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        var dream = _story.RenderDreamRoute(code.Result, code.Progress);
        return (new PupilDreamJobResponse(storeKey, dream), null, 200);
    }

    public async Task<(byte[]? Bytes, string? FileName, PupilErrorDto? Error, int StatusCode)> BuildPdfAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveSessionAsync(pupil, cancellationToken);
        if (ctx.Error is not null)
        {
            return (null, null, ctx.Error, ctx.StatusCode);
        }

        var code = ctx.Code!;
        var schoolClass = ctx.Class!;
        if (code.Status != PupilCodeStatus.Completed || code.Result is null)
        {
            return (null, null, new PupilErrorDto("not_completed", "Je reis is nog niet klaar."), 409);
        }

        var schoolName = await _db.Schools.AsNoTracking()
            .Where(s => s.Id == schoolClass.SchoolId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "";
        var display = pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value ?? "";
        var model = BuildPdfModel(code, schoolClass.Name, schoolName, display);
        var bytes = _pdf.Render(model);
        var fileName = $"lobsy-ontdekkingsreis-{SanitizeFilePart(schoolClass.Name)}.pdf";
        return (bytes, fileName, null, 200);
    }

    private PupilReportPdfModel BuildPdfModel(
        PupilCode code,
        string className,
        string schoolName,
        string displayCode)
    {
        var result = code.Result!;
        var story = _story.Render(result, code.Progress);
        var dream = _story.RenderDreamRoute(result, code.Progress);
        var likes = ResolveChipLabels(ParseTagList(code.Progress?.LikesJson));
        var dislikes = ResolveChipLabels(ParseTagList(code.Progress?.DislikesJson));
        if (!string.IsNullOrWhiteSpace(code.Progress?.LikeOtherWord))
        {
            likes = likes.Concat([code.Progress!.LikeOtherWord!]).ToList();
        }

        if (!string.IsNullOrWhiteSpace(code.Progress?.DislikeOtherWord))
        {
            dislikes = dislikes.Concat([code.Progress!.DislikeOtherWord!]).ToList();
        }

        var date = DateOnly.FromDateTime(result.CompletedAtUtc == default
            ? _clock.GetUtcNow().UtcDateTime
            : result.CompletedAtUtc);

        return new PupilReportPdfModel(
            SchoolName: schoolName,
            ClassName: className,
            DisplayCode: displayCode,
            Date: date,
            StoryBody: story.Body,
            Tiles: story.Tiles.Select(t => (t.KidLabel, t.Explanation)).ToList(),
            Likes: likes,
            Dislikes: dislikes,
            JobIdeas: story.JobIdeas,
            DreamJobTitle: string.IsNullOrWhiteSpace(dream.JobKey)
                           || string.Equals(dream.JobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase)
                ? null
                : dream.JobTitle,
            RouteSteps: dream.RouteSteps,
            Encouragement: dream.Encouragement);
    }

    private static List<string> ResolveChipLabels(IReadOnlyList<string> keys)
        => keys.Select(ChipDutch).ToList();

    private static string ChipDutch(string key) => key switch
    {
        "sport" => "Sport",
        "buiten" => "Buiten zijn",
        "dieren" => "Dieren",
        "gamen" => "Gamen",
        "tekenen" => "Tekenen",
        "muziek" => "Muziek",
        "koken" => "Koken of bakken",
        "fietsen-repareren" => "Fietsen repareren",
        "bouwen" => "Bouwen & knutselen",
        "techniek" => "Techniek",
        "lezen" => "Lezen",
        "dansen" => "Dansen",
        "theater" => "Theater",
        "filmpjes" => "Filmpjes maken",
        "mode" => "Mode",
        "kleine-kinderen" => "Kleine kinderen",
        "natuur" => "Natuur",
        "autos" => "Auto's & motoren",
        "computers" => "Computers",
        "puzzels" => "Puzzels",
        "rekenen" => "Rekenen",
        "talen" => "Talen",
        "reizen" => "Reizen",
        "programmeren" => "Programmeren",
        "voor-de-klas" => "Voor de klas praten",
        "lang-stilzitten" => "Lang stilzitten",
        "hard-werken-kou" => "Hard werken in de kou",
        "veel-lezen" => "Veel lezen",
        "alleen-werken" => "Alleen werken",
        "druk-lawaai" => "Druk en lawaai",
        "vies-worden" => "Vies worden",
        _ => key
    };

    private static string SanitizeFilePart(string name)
    {
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "klas" : cleaned.ToLowerInvariant();
    }

    private static (List<string> Likes, List<string> Dislikes, string? LikeOther, string? DislikeOther, PupilErrorDto? Error)
        ValidateChips(PupilChipsRequest request)
    {
        var allowedLike = PupilInterestChipCatalog.LikeChips.Select(c => c.Key).ToHashSet(StringComparer.Ordinal);
        var allowedDislike = PupilInterestChipCatalog.DislikeChips.Select(c => c.Key).ToHashSet(StringComparer.Ordinal);

        var likes = (request.Likes ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k) && allowedLike.Contains(k.Trim()))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var dislikes = (request.Dislikes ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k) && allowedDislike.Contains(k.Trim()))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Exclusivity: a chip can't be in both groups.
        var overlap = likes.Intersect(dislikes, StringComparer.Ordinal).ToList();
        if (overlap.Count > 0)
        {
            foreach (var key in overlap)
            {
                dislikes.Remove(key);
            }
        }

        var likeOther = NormalizeOther(request.LikeOtherWord, out var likeErr);
        if (likeErr is not null)
        {
            return ([], [], null, null, likeErr);
        }

        var dislikeOther = NormalizeOther(request.DislikeOtherWord, out var dislikeErr);
        if (dislikeErr is not null)
        {
            return ([], [], null, null, dislikeErr);
        }

        return (likes, dislikes, likeOther, dislikeOther, null);
    }

    private static string? NormalizeOther(string? raw, out PupilErrorDto? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Any(char.IsDigit) || trimmed.Contains('@', StringComparison.Ordinal))
        {
            error = new PupilErrorDto("invalid_other", "Gebruik alleen letters, spaties of een streepje (max. 24).");
            return null;
        }

        if (!PupilInterestChipCatalog.OtherWordPattern.IsMatch(trimmed))
        {
            error = new PupilErrorDto("invalid_other", "Gebruik alleen letters, spaties of een streepje (max. 24).");
            return null;
        }

        if (PupilNameGuard.LooksLikeName(trimmed))
        {
            error = new PupilErrorDto("name_rejected", "Dat lijkt op een naam. Kies liever een knop.");
            return null;
        }

        return trimmed.Length > PupilInterestChipCatalog.OtherWordMaxLength
            ? trimmed[..PupilInterestChipCatalog.OtherWordMaxLength]
            : trimmed;
    }

    public async Task<(bool Ok, PupilErrorDto? Error, int StatusCode)> ClearLoginPauseAsync(
        ClaimsPrincipal staff,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var canTeach = await _scope.CanTeachClassAsync(staff, classId, cancellationToken);
        var canManage = await _scope.CanManageClassAsync(staff, classId, cancellationToken);
        if (!canTeach && !canManage)
        {
            return (false, new PupilErrorDto("not_found", "Klas niet gevonden."), 404);
        }

        var schoolClass = await _db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (schoolClass is null)
        {
            return (false, new PupilErrorDto("not_found", "Klas niet gevonden."), 404);
        }

        schoolClass.LoginPausedUntilUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, 200);
    }

    private async Task MaybePauseClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var count = _protection.RecordClassHourFailure(classId);
        if (count < PupilLoginProtection.ClassHourFailLimit)
        {
            return;
        }

        var schoolClass = await _db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (schoolClass is null)
        {
            return;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        schoolClass.LoginPausedUntilUtc = now.Add(PupilLoginProtection.ClassPauseDuration);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning(
            "Pupil login paused for class {ClassId} ({ClassName}) until {UntilUtc} after {Count} failures/hour.",
            classId, schoolClass.Name, schoolClass.LoginPausedUntilUtc, count);
    }

    private async Task<(PupilCode? Code, SchoolClass? Class, PupilErrorDto? Error, int StatusCode)> ResolveSessionAsync(
        ClaimsPrincipal pupil,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(pupil.FindFirst(PupilClaimTypes.PupilCodeId)?.Value, out var codeId)
            || !int.TryParse(pupil.FindFirst(PupilClaimTypes.SessionVersion)?.Value, out var claimVersion))
        {
            return (null, null, new PupilErrorDto(SessionInvalidError, "Sessie ongeldig."), 401);
        }

        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .Include(c => c.SchoolClass)!.ThenInclude(sc => sc!.School)
            .FirstOrDefaultAsync(c => c.Id == codeId, cancellationToken);

        if (code?.SchoolClass?.School is null)
        {
            return (null, null, new PupilErrorDto(SessionInvalidError, "Sessie ongeldig."), 401);
        }

        if (!code.SchoolClass.School.IsActive)
        {
            return (null, null, new PupilErrorDto(SchoolInactiveError, "School is niet actief."), 401);
        }

        if (code.SessionVersion != claimVersion)
        {
            return (null, null, new PupilErrorDto(SessionInvalidError, "Sessie beëindigd."), 401);
        }

        return (code, code.SchoolClass, null, 200);
    }

    /// <summary>
    /// Self-heal: when every item is answered (or status is already Completed) but
    /// <see cref="PupilResult"/> is missing, retry <see cref="IPupilResultBuilder.BuildAsync"/> once.
    /// </summary>
    private async Task<bool> EnsureResultAsync(PupilCode code, CancellationToken cancellationToken)
    {
        if (code.Result is not null)
        {
            return true;
        }

        var answers = ParseAnswers(code.Progress?.AnswersJson);
        var total = _bank.AllItems.Count;
        var allAnswered = answers.Count >= total
                          && _bank.AllItems.All(i => answers.ContainsKey(i.Id));
        if (!allAnswered && code.Status != PupilCodeStatus.Completed)
        {
            return false;
        }

        try
        {
            await _results.BuildAsync(code.Id, cancellationToken);
            await _db.Entry(code).ReloadAsync(cancellationToken);
            if (code.Progress is not null)
            {
                await _db.Entry(code.Progress).ReloadAsync(cancellationToken);
            }

            await _db.Entry(code).Reference(c => c.Result).LoadAsync(cancellationToken);
            return code.Result is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pupil result builder failed for {CodeId}", code.Id);
            try
            {
                await _db.Entry(code).ReloadAsync(cancellationToken);
                if (code.Progress is not null)
                {
                    await _db.Entry(code.Progress).ReloadAsync(cancellationToken);
                }
            }
            catch
            {
                // Best-effort restore; the warning above already captured the builder failure.
            }

            return false;
        }
    }

    private static (PupilLoginResponse? Ok, PupilErrorDto? Error, int StatusCode) GenericFail()
        => (null, new PupilErrorDto(GenericCodeError,
            "Die code klopt niet bij deze klas. Kijk goed op je kaartje of vraag je leraar."), 400);

    private static Dictionary<string, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<string> ParseTagList(string? json)
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

    private int FirstUnansweredIndex(string answersJson, int total)
    {
        var answers = ParseAnswers(answersJson);
        for (var i = 0; i < total; i++)
        {
            var item = _bank.GetByGlobalIndex(i);
            if (item is null || !answers.ContainsKey(item.Id))
            {
                return i;
            }
        }

        return total;
    }

    private static string FormatLevel(SchoolLevel level) => level switch
    {
        SchoolLevel.VmboB => "vmbo-b",
        SchoolLevel.VmboK => "vmbo-k",
        SchoolLevel.VmboGt => "vmbo-gt",
        SchoolLevel.Mavo => "mavo",
        SchoolLevel.Havo => "havo",
        SchoolLevel.Vwo => "vwo",
        SchoolLevel.Mix => "mix",
        _ => "anders"
    };
}
