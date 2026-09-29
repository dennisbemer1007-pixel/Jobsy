using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
    private readonly TimeProvider _clock;
    private readonly ILogger<PupilPortalService> _logger;

    public PupilPortalService(
        JobsyDbContext db,
        IPupilCodeService codes,
        IPupilLoginProtection protection,
        IPupilQuestionBank bank,
        IPupilResultBuilder results,
        ISchoolScopeService scope,
        ILogger<PupilPortalService> logger,
        TimeProvider? clock = null)
    {
        _db = db;
        _codes = codes;
        _protection = protection;
        _bank = bank;
        _results = results;
        _scope = scope;
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

        var completed = code.Status == PupilCodeStatus.Completed
                        || code.Progress?.CompletedAtUtc is not null;
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
        var answers = ParseAnswers(code.Progress?.AnswersJson);
        var total = _bank.AllItems.Count;
        var answered = answers.Count;
        var currentIndex = FirstUnansweredIndex(code.Progress?.AnswersJson ?? "{}", total);
        var completed = code.Status == PupilCodeStatus.Completed
                        || code.Progress?.CompletedAtUtc is not null;
        var windowOpen = schoolClass.TestWindow == TestWindowState.Open;
        var item = completed ? null : _bank.GetByGlobalIndex(currentIndex);
        var plates = PupilWorldCatalog.PlatesShed(answered);

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
            item?.WorldKey,
            answers,
            windowOpen,
            completed), null, 200);
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
        var completed = answers.Count >= total
                        && _bank.AllItems.All(i => answers.ContainsKey(i.Id));

        string? nextId = null;
        string? nextWorld = null;
        if (completed)
        {
            progress.CompletedAtUtc = now;
            code.Status = PupilCodeStatus.Completed;
            await _db.SaveChangesAsync(cancellationToken);
            try
            {
                await _results.BuildAsync(code.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pupil result builder failed for {CodeId}", code.Id);
            }
        }
        else
        {
            await _db.SaveChangesAsync(cancellationToken);
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
            nextWorld), null, 200);
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
