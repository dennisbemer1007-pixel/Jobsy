using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public interface ISchoolPortalService
{
    Task EnsureTestWindowsClosedAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<SchoolDashboardDto> GetDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolTodoItemDto>> GetTodosAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolPortalClassListItemDto>> ListClassesAsync(
        ClaimsPrincipal user,
        int? schoolYearStart,
        CancellationToken cancellationToken = default);

    Task<SchoolPortalClassDetailDto?> GetClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error)> CreateClassAsync(
        ClaimsPrincipal user,
        CreateSchoolClassRequest request,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error)> UpdateClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        UpdateSchoolClassRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        string confirmName,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error)> AddCodesAsync(
        ClaimsPrincipal user,
        Guid classId,
        int count,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalCodeRowDto? Row, string? Error)> ReplaceCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListPdfAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListCsvAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetParentalConfirmationAsync(
        ClaimsPrincipal user,
        Guid classId,
        bool confirmed,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetTestWindowAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        DateOnly? closesOn,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalResultsDto? Results, string? Error)> GetResultsAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolPortalTeacherListItemDto>> ListTeachersAsync(
        ClaimsPrincipal user,
        Guid? classIdFilter,
        CancellationToken cancellationToken = default);

    Task<(SchoolStaffInviteResultDto? Result, string? Error, string? ErrorCode)> InviteTeacherAsync(
        ClaimsPrincipal user,
        InviteTeacherRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> AssignTeacherClassesAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        IReadOnlyList<Guid> classIds,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> RemoveTeacherAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> ResendTeacherInviteAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        CancellationToken cancellationToken = default);

    Task<SchoolProfileDto?> GetProfileAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<SchoolPrivacyDto?> GetPrivacyAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed class SchoolPortalService : ISchoolPortalService
{
    public const int MaxCodesPerClass = 40;
    public const int ProgressTotalQuestions = 60;
    private static readonly Regex ClassNameRegex = new(
        @"^[\p{L}\d\- ]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();

    private static TimeZoneInfo ResolveAmsterdam()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }

    private readonly JobsyDbContext _db;
    private readonly ISchoolScopeService _scope;
    private readonly IPupilCodeService _codes;
    private readonly ISchoolCodeListPdfService _pdf;
    private readonly ISchoolStaffInviteService _invites;
    private readonly IPlatformFeatureService _features;
    private readonly IPersonalDataAccessLogger _accessLog;

    public SchoolPortalService(
        JobsyDbContext db,
        ISchoolScopeService scope,
        IPupilCodeService codes,
        ISchoolCodeListPdfService pdf,
        ISchoolStaffInviteService invites,
        IPlatformFeatureService features,
        IPersonalDataAccessLogger accessLog)
    {
        _db = db;
        _scope = scope;
        _codes = codes;
        _pdf = pdf;
        _invites = invites;
        _features = features;
        _accessLog = accessLog;
    }

    public async Task EnsureTestWindowsClosedAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        var today = TodayAmsterdam();
        var open = await _db.SchoolClasses
            .Where(c => c.SchoolId == schoolId
                        && c.TestWindow == TestWindowState.Open
                        && c.TestWindowClosesOn != null
                        && c.TestWindowClosesOn < today)
            .ToListAsync(cancellationToken);
        if (open.Count == 0)
        {
            return;
        }

        foreach (var c in open)
        {
            c.TestWindow = TestWindowState.Closed;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SchoolDashboardDto> GetDashboardAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        await EnsureTestWindowsClosedAsync(schoolId, cancellationToken);
        var school = await _db.Schools.AsNoTracking().FirstAsync(s => s.Id == schoolId, cancellationToken);
        var snap = await _features.GetAsync(cancellationToken);
        var yearStart = SchoolYear.Current(TodayAmsterdam(), snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == yearStart)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.PupilCodes)
            .OrderBy(c => c.Year).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var teacherIds = classes.SelectMany(c => c.TeacherAssignments.Select(a => a.TeacherUserId)).Distinct().ToList();
        var teachers = await _db.Users.AsNoTracking()
            .Where(u => u.SchoolId == schoolId && (u.Role == UserRole.Teacher || u.Role == UserRole.SchoolAdmin))
            .ToListAsync(cancellationToken);
        var teacherMap = teachers.ToDictionary(t => t.Id);

        var codeCount = classes.Sum(c => c.PupilCodes.Count);
        var completed = classes.Sum(c => c.PupilCodes.Count(p => p.Status == PupilCodeStatus.Completed));
        var withoutMfa = teachers.Count(t => t.IsActive && !t.AuthenticatorEnabled && t.Role == UserRole.Teacher);

        var classRows = classes.Select(c =>
        {
            var names = c.TeacherAssignments
                .Select(a => teacherMap.GetValueOrDefault(a.TeacherUserId)?.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToList();
            var codes = c.PupilCodes.Count;
            var done = c.PupilCodes.Count(p => p.Status == PupilCodeStatus.Completed);
            var started = c.PupilCodes.Count(p => p.Status is PupilCodeStatus.InProgress or PupilCodeStatus.Completed);
            return new SchoolDashboardClassRowDto(
                c.Id,
                c.Name,
                names.Count == 0 ? null : string.Join(", ", names),
                codes,
                started,
                done,
                codes == 0 ? 0 : Math.Round(100d * done / codes, 0),
                c.TestWindow,
                c.TestWindowClosesOn);
        }).ToList();

        var todos = await BuildTodosInternalAsync(schoolId, classes, snap, maxItems: 5, cancellationToken);

        IReadOnlyList<NamedCountDto>? riasec = null;
        var completedIds = classes.SelectMany(c => c.PupilCodes)
            .Where(p => p.Status == PupilCodeStatus.Completed)
            .Select(p => p.Id)
            .ToList();
        if (completedIds.Count >= SchoolAnonymity.MinGroupSize)
        {
            var results = await _db.PupilResults.AsNoTracking()
                .Where(r => completedIds.Contains(r.PupilCodeId))
                .ToListAsync(cancellationToken);
            riasec = ClassResultsAggregator.SchoolRiasecTop3(results)
                .Select(n => new NamedCountDto(n.Key, n.Count))
                .ToList();
        }

        return new SchoolDashboardDto(
            school.Name,
            school.City,
            SchoolYear.Label(yearStart),
            classes.Count,
            codeCount,
            completed,
            codeCount == 0 ? 0 : Math.Round(100d * completed / codeCount, 0),
            teachers.Count(t => t.IsActive && t.Role == UserRole.Teacher),
            withoutMfa,
            classRows,
            todos,
            riasec);
    }

    public async Task<IReadOnlyList<SchoolTodoItemDto>> GetTodosAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        await EnsureTestWindowsClosedAsync(schoolId, cancellationToken);
        var snap = await _features.GetAsync(cancellationToken);
        var yearStart = SchoolYear.Current(TodayAmsterdam(), snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);
        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == yearStart)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.PupilCodes)
            .ToListAsync(cancellationToken);
        return await BuildTodosInternalAsync(schoolId, classes, snap, maxItems: int.MaxValue, cancellationToken);
    }

    public async Task<IReadOnlyList<SchoolPortalClassListItemDto>> ListClassesAsync(
        ClaimsPrincipal user,
        int? schoolYearStart,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        await EnsureTestWindowsClosedAsync(schoolId, cancellationToken);
        var snap = await _features.GetAsync(cancellationToken);
        var year = schoolYearStart
                   ?? SchoolYear.Current(TodayAmsterdam(), snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == year)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.PupilCodes)
            .OrderBy(c => c.Year).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var teacherIds = classes.SelectMany(c => c.TeacherAssignments.Select(a => a.TeacherUserId)).Distinct().ToList();
        var teacherNames = await _db.Users.AsNoTracking()
            .Where(u => teacherIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return classes.Select(c => new SchoolPortalClassListItemDto(
            c.Id,
            c.Name,
            c.Level,
            c.Year,
            c.SchoolYearStart,
            SchoolYear.Label(c.SchoolYearStart),
            c.TeacherAssignments
                .Select(a => teacherNames.GetValueOrDefault(a.TeacherUserId) ?? "")
                .Where(n => n.Length > 0)
                .ToList(),
            c.PupilCodes.Count,
            c.PupilCodes.Count(p => p.Status == PupilCodeStatus.Completed),
            c.ParentalInfoConfirmedAtUtc != null,
            c.TestWindow,
            c.TestWindowClosesOn)).ToList();
    }

    public async Task<SchoolPortalClassDetailDto?> GetClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        await EnsureTestWindowsClosedAsync(schoolId, cancellationToken);
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        return await MapClassDetailAsync(classId, cancellationToken);
    }

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error)> CreateClassAsync(
        ClaimsPrincipal user,
        CreateSchoolClassRequest request,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        if (!RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin))
        {
            return (null, "forbidden");
        }

        var nameError = ValidateClassName(request.ClassName);
        if (nameError is not null)
        {
            return (null, nameError);
        }

        if (request.Year is < 1 or > 6)
        {
            return (null, "Leerjaar moet 1–6 zijn.");
        }

        if (request.PupilCount is < 1 or > MaxCodesPerClass)
        {
            return (null, $"Aantal leerlingen moet 1–{MaxCodesPerClass} zijn.");
        }

        var snap = await _features.GetAsync(cancellationToken);
        var yearStart = request.SchoolYearStart
                        ?? SchoolYear.Current(TodayAmsterdam(), snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);
        var name = request.ClassName.Trim();

        if (await _db.SchoolClasses.AnyAsync(
                c => c.SchoolId == schoolId && c.SchoolYearStart == yearStart && c.Name == name,
                cancellationToken))
        {
            return (null, "Er bestaat al een klas met deze naam in dit schooljaar.");
        }

        var teacherIds = (request.TeacherUserIds ?? []).Distinct().ToList();
        if (teacherIds.Count > 0)
        {
            var ok = await TeachersBelongToSchoolAsync(schoolId, teacherIds, cancellationToken);
            if (!ok)
            {
                return (null, "Een of meer leraren horen niet bij deze school.");
            }
        }

        var entity = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Level = request.Level,
            Year = request.Year,
            SchoolYearStart = yearStart,
            PupilCount = request.PupilCount,
            TestWindow = TestWindowState.NotOpen,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.SchoolClasses.Add(entity);
        foreach (var tid in teacherIds)
        {
            _db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = tid,
                SchoolClassId = entity.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _codes.GenerateAsync(request.PupilCount, entity, cancellationToken);
        return (await MapClassDetailAsync(entity.Id, cancellationToken), null);
    }

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error)> UpdateClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        UpdateSchoolClassRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found");
        }

        var nameError = ValidateClassName(request.ClassName);
        if (nameError is not null)
        {
            return (null, nameError);
        }

        if (request.Year is < 1 or > 6)
        {
            return (null, "Leerjaar moet 1–6 zijn.");
        }

        var entity = await _db.SchoolClasses
            .Include(c => c.TeacherAssignments)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return (null, "not_found");
        }

        var name = request.ClassName.Trim();
        if (await _db.SchoolClasses.AnyAsync(
                c => c.SchoolId == entity.SchoolId
                     && c.SchoolYearStart == entity.SchoolYearStart
                     && c.Name == name
                     && c.Id != classId,
                cancellationToken))
        {
            return (null, "Er bestaat al een klas met deze naam in dit schooljaar.");
        }

        var teacherIds = (request.TeacherUserIds ?? []).Distinct().ToList();
        if (teacherIds.Count > 0
            && !await TeachersBelongToSchoolAsync(entity.SchoolId, teacherIds, cancellationToken))
        {
            return (null, "Een of meer leraren horen niet bij deze school.");
        }

        entity.Name = name;
        entity.Level = request.Level;
        entity.Year = request.Year;

        _db.TeacherClassAssignments.RemoveRange(entity.TeacherAssignments);
        foreach (var tid in teacherIds)
        {
            _db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = tid,
                SchoolClassId = entity.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await MapClassDetailAsync(classId, cancellationToken), null);
    }

    public async Task<(bool Ok, string? Error)> DeleteClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        string confirmName,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (false, "not_found");
        }

        var entity = await _db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return (false, "not_found");
        }

        if (!string.Equals(confirmName?.Trim(), entity.Name, StringComparison.Ordinal))
        {
            return (false, "Typ de klasnaam ter bevestiging.");
        }

        _db.SchoolClasses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error)> AddCodesAsync(
        ClaimsPrincipal user,
        Guid classId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found");
        }

        if (count < 1)
        {
            return (null, "Aantal moet minstens 1 zijn.");
        }

        var entity = await _db.SchoolClasses
            .Include(c => c.PupilCodes)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return (null, "not_found");
        }

        var current = entity.PupilCodes.Count;
        if (current + count > MaxCodesPerClass)
        {
            return (null, $"Maximaal {MaxCodesPerClass} codes per klas (nu {current}).");
        }

        await _codes.GenerateAsync(count, entity, cancellationToken);
        entity.PupilCount = current + count;
        await _db.SaveChangesAsync(cancellationToken);
        return (await MapClassDetailAsync(classId, cancellationToken), null);
    }

    public async Task<(SchoolPortalCodeRowDto? Row, string? Error)> ReplaceCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found");
        }

        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .FirstOrDefaultAsync(c => c.Id == codeId && c.SchoolClassId == classId, cancellationToken);
        if (code is null)
        {
            return (null, "not_found");
        }

        await _codes.ReplaceAsync(code, cancellationToken);
        return (MapCodeRow(code), null);
    }

    public async Task<(bool Ok, string? Error)> DeleteCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (false, "not_found");
        }

        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .FirstOrDefaultAsync(c => c.Id == codeId && c.SchoolClassId == classId, cancellationToken);
        if (code is null)
        {
            return (false, "not_found");
        }

        if (code.Progress is not null)
        {
            _db.PupilProgresses.Remove(code.Progress);
        }

        if (code.Result is not null)
        {
            _db.PupilResults.Remove(code.Result);
        }

        _db.PupilCodes.Remove(code);

        var schoolClass = await _db.SchoolClasses.FirstAsync(c => c.Id == classId, cancellationToken);
        schoolClass.PupilCount = Math.Max(0, schoolClass.PupilCount - 1);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListPdfAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var pack = await LoadCodeListRowsAsync(user, classId, cancellationToken);
        if (pack is null)
        {
            return (null, null, "not_found");
        }

        var bytes = _pdf.Render(pack.Value.SchoolName, pack.Value.ClassName, pack.Value.YearLabel, pack.Value.Rows);
        await LogCodelistAccessAsync(user, classId, "pdf", cancellationToken);
        return (bytes, $"codelijst-{SanitizeFile(pack.Value.ClassName)}.pdf", null);
    }

    public async Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListCsvAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var pack = await LoadCodeListRowsAsync(user, classId, cancellationToken);
        if (pack is null)
        {
            return (null, null, "not_found");
        }

        var bytes = SchoolCodeListCsv.Build(pack.Value.Rows);
        await LogCodelistAccessAsync(user, classId, "csv", cancellationToken);
        return (bytes, $"codelijst-{SanitizeFile(pack.Value.ClassName)}.csv", null);
    }

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetParentalConfirmationAsync(
        ClaimsPrincipal user,
        Guid classId,
        bool confirmed,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found", "not_found");
        }

        var entity = await _db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return (null, "not_found", "not_found");
        }

        var userId = GetUserId(user);
        if (confirmed)
        {
            entity.ParentalInfoConfirmedAtUtc = DateTime.UtcNow;
            entity.ParentalInfoConfirmedByUserId = userId;
            entity.ParentalInfoTextVersion = ParentalInfoTexts.CurrentVersion;
        }
        else
        {
            if (entity.TestWindow != TestWindowState.NotOpen)
            {
                return (null, "Bevestiging intrekken kan alleen zolang het testvenster nog nooit open is geweest.", "window_already_opened");
            }

            entity.ParentalInfoConfirmedAtUtc = null;
            entity.ParentalInfoConfirmedByUserId = null;
            entity.ParentalInfoTextVersion = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await MapClassDetailAsync(classId, cancellationToken), null, null);
    }

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetTestWindowAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        DateOnly? closesOn,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found", "not_found");
        }

        var entity = await _db.SchoolClasses
            .Include(c => c.School)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return (null, "not_found", "not_found");
        }

        var act = (action ?? "").Trim().ToLowerInvariant();
        if (act is "open" or "reopen")
        {
            if (entity.School?.ProcessorAgreementSignedOn is null)
            {
                return (null,
                    "Eerst moet de verwerkersovereenkomst geregistreerd zijn.",
                    "processor_agreement_missing");
            }

            if (entity.ParentalInfoConfirmedAtUtc is null)
            {
                return (null,
                    "Bevestig eerst dat ouders zijn geïnformeerd.",
                    "parental_info_missing");
            }

            var snap = await _features.GetAsync(cancellationToken);
            var today = TodayAmsterdam();
            var cutoff = SchoolYear.EndsOn(
                entity.SchoolYearStart,
                snap.SchoolRetentionCutoffMonth,
                snap.SchoolRetentionCutoffDay);

            if (closesOn is DateOnly close)
            {
                if (close < today)
                {
                    return (null, "Sluitdatum moet vandaag of later zijn.", "invalid_closes_on");
                }

                if (close > cutoff)
                {
                    return (null, $"Sluitdatum mag niet na {cutoff:dd-MM-yyyy} liggen.", "invalid_closes_on");
                }

                entity.TestWindowClosesOn = close;
            }
            else if (act == "open")
            {
                entity.TestWindowClosesOn = null;
            }

            entity.TestWindow = TestWindowState.Open;
        }
        else if (act == "close")
        {
            entity.TestWindow = TestWindowState.Closed;
        }
        else
        {
            return (null, "Onbekende actie. Gebruik open, close of reopen.", "invalid_action");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await MapClassDetailAsync(classId, cancellationToken), null, null);
    }

    public async Task<(SchoolPortalResultsDto? Results, string? Error)> GetResultsAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        // api/school is SchoolAdmin-only; foreign class → 404 via manage check.
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found");
        }

        var schoolClass = await _db.SchoolClasses.AsNoTracking()
            .Include(c => c.PupilCodes)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (schoolClass is null)
        {
            return (null, "not_found");
        }

        var results = await _db.PupilResults.AsNoTracking()
            .Where(r => r.SchoolClassId == classId)
            .ToListAsync(cancellationToken);
        var totals = ClassResultsAggregator.Aggregate(results, schoolClass.PupilCodes.Count);

        var snap = await _features.GetAsync(cancellationToken);
        // D4: when setting off, perCode is null (server-enforced — not just UI-hidden).
        IReadOnlyList<SchoolPortalPerCodeResultDto>? perCode = null;
        if (snap.SchoolPerCodeResultsEnabled)
        {
            var byCode = results.ToDictionary(r => r.PupilCodeId);
            perCode = schoolClass.PupilCodes
                .OrderBy(c => c.Number)
                .Select(c =>
                {
                    byCode.TryGetValue(c.Id, out var r);
                    var display = _codes.Unprotect(c.CodeProtected) is { } raw
                        ? PupilCodeFormat.Display(raw)
                        : "******";
                    return new SchoolPortalPerCodeResultDto(
                        c.Id,
                        display,
                        c.Status,
                        r?.HollandCode,
                        r?.TopValue,
                        r?.DreamJobKey);
                })
                .ToList();
        }

        return (new SchoolPortalResultsDto(
            classId,
            schoolClass.Name,
            snap.SchoolPerCodeResultsEnabled,
            totals,
            perCode), null);
    }

    public async Task<IReadOnlyList<SchoolPortalTeacherListItemDto>> ListTeachersAsync(
        ClaimsPrincipal user,
        Guid? classIdFilter,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var teachers = await _db.Users.AsNoTracking()
            .Where(u => u.SchoolId == schoolId && u.Role == UserRole.Teacher)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        var ids = teachers.Select(t => t.Id).ToList();
        var assignments = await _db.TeacherClassAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.TeacherUserId))
            .Join(_db.SchoolClasses.AsNoTracking(),
                a => a.SchoolClassId,
                c => c.Id,
                (a, c) => new { a.TeacherUserId, c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var pending = await _db.SchoolStaffInvites.AsNoTracking()
            .Where(i => i.SchoolId == schoolId
                        && i.Role == JobsyRoles.Teacher
                        && i.AcceptedAtUtc == null
                        && i.RevokedAtUtc == null
                        && i.ExpiresAtUtc > DateTime.UtcNow
                        && i.InvitedUserId != null)
            .Select(i => i.InvitedUserId!.Value)
            .ToListAsync(cancellationToken);
        var pendingSet = pending.ToHashSet();

        var list = teachers.Select(t =>
        {
            var classes = assignments
                .Where(a => a.TeacherUserId == t.Id)
                .Select(a => new SchoolPortalTeacherClassChipDto(a.Id, a.Name))
                .OrderBy(c => c.ClassName)
                .ToList();
            return new SchoolPortalTeacherListItemDto(
                t.Id,
                t.FullName,
                t.Email,
                classes,
                t.AuthenticatorEnabled,
                pendingSet.Contains(t.Id) || (!t.AuthenticatorEnabled && t.LastLoginAtUtc is null),
                t.LastLoginAtUtc,
                t.IsActive);
        }).ToList();

        if (classIdFilter is Guid filter)
        {
            list = list.Where(t => t.Classes.Any(c => c.ClassId == filter)).ToList();
        }

        return list;
    }

    public async Task<(SchoolStaffInviteResultDto? Result, string? Error, string? ErrorCode)> InviteTeacherAsync(
        ClaimsPrincipal user,
        InviteTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin))
        {
            return (null, "forbidden", "forbidden");
        }

        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var actorId = GetUserId(user) ?? Guid.Empty;
        var classIds = (request.ClassIds ?? []).Distinct().ToList();
        if (classIds.Count > 0)
        {
            var count = await _db.SchoolClasses.CountAsync(
                c => c.SchoolId == schoolId && classIds.Contains(c.Id), cancellationToken);
            if (count != classIds.Count)
            {
                return (null, "Klassen moeten tot deze school behoren.", "invalid_class");
            }
        }

        try
        {
            var result = await _invites.InviteTeacherAsync(
                schoolId,
                request.FullName,
                request.Email,
                classIds,
                actorId,
                cancellationToken);
            return (new SchoolStaffInviteResultDto(
                result.InviteId,
                result.UserId,
                result.Email,
                result.Role,
                result.ExpiresAtUtc), null, null);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email_domain_not_allowed", StringComparison.Ordinal))
        {
            return (null, ex.Message, "email_domain_not_allowed");
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message, "invite_failed");
        }
    }

    public async Task<(bool Ok, string? Error)> AssignTeacherClassesAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        IReadOnlyList<Guid> classIds,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var teacher = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == teacherUserId && u.SchoolId == schoolId
                 && (u.Role == UserRole.Teacher || u.Role == UserRole.SchoolAdmin),
            cancellationToken);
        if (teacher is null)
        {
            return (false, "not_found");
        }

        var ids = (classIds ?? []).Distinct().ToList();
        if (ids.Count > 0)
        {
            var count = await _db.SchoolClasses.CountAsync(
                c => c.SchoolId == schoolId && ids.Contains(c.Id), cancellationToken);
            if (count != ids.Count)
            {
                return (false, "Klassen moeten tot deze school behoren.");
            }
        }

        var existing = await _db.TeacherClassAssignments
            .Where(a => a.TeacherUserId == teacherUserId)
            .ToListAsync(cancellationToken);
        _db.TeacherClassAssignments.RemoveRange(existing);
        foreach (var cid in ids)
        {
            _db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = teacherUserId,
                SchoolClassId = cid,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RemoveTeacherAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var teacher = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == teacherUserId && u.SchoolId == schoolId && u.Role == UserRole.Teacher,
            cancellationToken);
        if (teacher is null)
        {
            return (false, "not_found");
        }

        var assignments = await _db.TeacherClassAssignments
            .Where(a => a.TeacherUserId == teacherUserId)
            .ToListAsync(cancellationToken);
        _db.TeacherClassAssignments.RemoveRange(assignments);
        teacher.IsActive = false;
        teacher.SessionVersion++;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ResendTeacherInviteAsync(
        ClaimsPrincipal user,
        Guid teacherUserId,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var teacher = await _db.Users.AsNoTracking().FirstOrDefaultAsync(
            u => u.Id == teacherUserId && u.SchoolId == schoolId && u.Role == UserRole.Teacher,
            cancellationToken);
        if (teacher is null)
        {
            return (false, "not_found");
        }

        var classIds = await _db.TeacherClassAssignments.AsNoTracking()
            .Where(a => a.TeacherUserId == teacherUserId)
            .Select(a => a.SchoolClassId)
            .ToListAsync(cancellationToken);
        var actorId = GetUserId(user) ?? Guid.Empty;
        try
        {
            await _invites.InviteTeacherAsync(
                schoolId, teacher.FullName, teacher.Email, classIds, actorId, cancellationToken);
            return (true, null);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<SchoolProfileDto?> GetProfileAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return null;
        }

        var domains = ParseDomains(school.AllowedEmailDomains);
        return new SchoolProfileDto(
            school.Id,
            school.Name,
            school.City,
            school.BrinCode,
            domains,
            school.ProcessorAgreementSignedOn,
            school.ProcessorAgreementVersion,
            school.IsActive);
    }

    public async Task<SchoolPrivacyDto?> GetPrivacyAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var schoolId = _scope.GetSchoolIdOrThrow(user);
        var school = await _db.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken);
        if (school is null)
        {
            return null;
        }

        var snap = await _features.GetAsync(cancellationToken);
        var today = TodayAmsterdam();
        var yearStart = SchoolYear.Current(today, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);
        var cutoff = SchoolYear.EndsOn(yearStart, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == yearStart)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        var confirmerIds = classes
            .Where(c => c.ParentalInfoConfirmedByUserId is not null)
            .Select(c => c.ParentalInfoConfirmedByUserId!.Value)
            .Distinct()
            .ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => confirmerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return new SchoolPrivacyDto(
            school.ProcessorAgreementSignedOn,
            school.ProcessorAgreementVersion,
            cutoff,
            SchoolYear.Label(yearStart),
            classes.Select(c => new SchoolPrivacyClassConfirmationDto(
                c.Id,
                c.Name,
                c.ParentalInfoConfirmedAtUtc != null,
                c.ParentalInfoConfirmedAtUtc,
                c.ParentalInfoConfirmedByUserId is Guid id
                    ? names.GetValueOrDefault(id)
                    : null)).ToList(),
            OuderbriefTemplate.DutchText);
    }

    private async Task<IReadOnlyList<SchoolTodoItemDto>> BuildTodosInternalAsync(
        Guid schoolId,
        List<SchoolClass> classes,
        PlatformFeatureSnapshot snap,
        int maxItems,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = TodayAmsterdam();
        var pending = await _db.SchoolStaffInvites.AsNoTracking()
            .Where(i => i.SchoolId == schoolId
                        && i.Role == JobsyRoles.Teacher
                        && i.AcceptedAtUtc == null
                        && i.RevokedAtUtc == null
                        && i.ExpiresAtUtc > now
                        && i.CreatedAtUtc <= now.AddDays(-3))
            .ToListAsync(cancellationToken);

        var input = new SchoolTodoInput(
            now,
            today,
            SchoolYear.EndsOn(
                SchoolYear.Current(today, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay),
                snap.SchoolRetentionCutoffMonth,
                snap.SchoolRetentionCutoffDay),
            classes.Select(c => new SchoolTodoClassInput(
                c.Id,
                c.Name,
                c.TeacherAssignments.Count > 0,
                c.ParentalInfoConfirmedAtUtc != null,
                c.TestWindow,
                c.TestWindowClosesOn,
                c.PupilCodes.Count,
                c.PupilCodes.Count(p => p.Status == PupilCodeStatus.Completed),
                c.LoginPausedUntilUtc)).ToList(),
            pending.Select(i => new SchoolTodoInviteInput(
                i.Id,
                i.InvitedUserId,
                i.FullName,
                i.CreatedAtUtc)).ToList());

        return SchoolTodoBuilder.Build(input, maxItems)
            .Select(t => new SchoolTodoItemDto(
                t.Kind.ToString(),
                t.Title,
                t.Href,
                t.ClassId,
                t.TeacherUserId,
                t.DueOn))
            .ToList();
    }

    private async Task<SchoolPortalClassDetailDto?> MapClassDetailAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var entity = await _db.SchoolClasses.AsNoTracking()
            .Include(c => c.School)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.PupilCodes).ThenInclude(p => p.Progress)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var teacherIds = entity.TeacherAssignments.Select(a => a.TeacherUserId).ToList();
        var teachers = await _db.Users.AsNoTracking()
            .Where(u => teacherIds.Contains(u.Id))
            .Select(u => new SchoolPortalTeacherChipDto(u.Id, u.FullName))
            .ToListAsync(cancellationToken);

        string? confirmer = null;
        if (entity.ParentalInfoConfirmedByUserId is Guid cid)
        {
            confirmer = await _db.Users.AsNoTracking()
                .Where(u => u.Id == cid)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var codes = entity.PupilCodes
            .OrderBy(c => c.Number)
            .Select(MapCodeRow)
            .ToList();

        return new SchoolPortalClassDetailDto(
            entity.Id,
            entity.Name,
            entity.Level,
            entity.Year,
            entity.SchoolYearStart,
            SchoolYear.Label(entity.SchoolYearStart),
            entity.PupilCount,
            teachers,
            entity.ParentalInfoConfirmedAtUtc != null,
            entity.ParentalInfoConfirmedAtUtc,
            confirmer,
            entity.ParentalInfoTextVersion,
            entity.TestWindow,
            entity.TestWindowClosesOn,
            entity.School?.ProcessorAgreementSignedOn != null,
            codes);
    }

    private SchoolPortalCodeRowDto MapCodeRow(PupilCode c)
    {
        var display = _codes.Unprotect(c.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";
        var current = c.Status switch
        {
            PupilCodeStatus.Completed => ProgressTotalQuestions,
            PupilCodeStatus.InProgress => c.Progress?.CurrentIndex ?? 0,
            _ => 0
        };
        return new SchoolPortalCodeRowDto(
            c.Id,
            c.Number,
            display,
            c.Status,
            current,
            ProgressTotalQuestions,
            c.LastSeenAtUtc);
    }

    private async Task<(string SchoolName, string ClassName, string YearLabel, IReadOnlyList<SchoolCodeListRow> Rows)?>
        LoadCodeListRowsAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken)
    {
        if (!await _scope.CanManageClassAsync(user, classId, cancellationToken)
            && !await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var entity = await _db.SchoolClasses.AsNoTracking()
            .Include(c => c.School)
            .Include(c => c.PupilCodes)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (entity?.School is null)
        {
            return null;
        }

        var rows = entity.PupilCodes
            .OrderBy(c => c.Number)
            .Select(c =>
            {
                var display = _codes.Unprotect(c.CodeProtected) is { } raw
                    ? PupilCodeFormat.Display(raw)
                    : "******";
                return new SchoolCodeListRow(c.Number, display);
            })
            .ToList();

        return (entity.School.Name, entity.Name, SchoolYear.Label(entity.SchoolYearStart), rows);
    }

    private async Task LogCodelistAccessAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user) ?? Guid.Empty;
        var role = RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
            ? JobsyRoles.SchoolAdmin
            : JobsyRoles.Teacher;
        await _accessLog.LogAsync(new PersonalDataAccessEntry(
            userId,
            role,
            "school.codelist",
            action,
            Reason: $"classId={classId:D}"), cancellationToken);
    }

    private async Task<bool> TeachersBelongToSchoolAsync(
        Guid schoolId,
        IReadOnlyList<Guid> teacherIds,
        CancellationToken cancellationToken)
    {
        var count = await _db.Users.CountAsync(
            u => teacherIds.Contains(u.Id)
                 && u.SchoolId == schoolId
                 && (u.Role == UserRole.Teacher || u.Role == UserRole.SchoolAdmin)
                 && u.IsActive,
            cancellationToken);
        return count == teacherIds.Count;
    }

    private static string? ValidateClassName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Klasnaam is verplicht.";
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 12)
        {
            return "Klasnaam mag max. 12 tekens zijn.";
        }

        if (!ClassNameRegex.IsMatch(trimmed))
        {
            return "Klasnaam mag alleen letters, cijfers, spaties en streepjes bevatten.";
        }

        return null;
    }

    private static IReadOnlyList<string> ParseDomains(string json)
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

    private static string SanitizeFile(string name)
    {
        var chars = name.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray();
        return chars.Length == 0 ? "klas" : new string(chars);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static DateOnly TodayAmsterdam()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Amsterdam);
        return DateOnly.FromDateTime(local);
    }
}

/// <summary>Static Dutch ouderbrief template (copy + PDF on privacy page).</summary>
public static class OuderbriefTemplate
{
    public const string DutchText =
        """
        Beste ouder(s)/verzorger(s),

        Op school gaan we met Lobsy werken: een digitale ontdekkingstocht waarmee leerlingen hun interesses, drijfveren en een mogelijke droombaan verkennen. De test bestaat uit 60 kindvriendelijke vragen en past in één lesuur.

        Belangrijk:
        • Lobsy bewaart geen namen. Iedere leerling krijgt een code van school. Alleen school houdt de koppeling tussen code en naam.
        • Er is geen kunstmatige intelligentie op leerlinggegevens, geen vacatures, geen reclame en geen werkgeverscontact.
        • De school is verwerkingsverantwoordelijke; Lobsy verwerkt de gegevens in opdracht (verwerker).
        • De schoolbeheerder ziet totalen per klas (vanaf 5 afgeronde tests). De leraar van de klas ziet de voortgang en (waar toegestaan) korte uitkomsten per code.
        • Individuele leerlinggegevens worden na het schooljaar automatisch verwijderd (standaard 31 juli). Anonieme totalen vanaf 5 leerlingen kunnen voor rapportage bewaard blijven.

        Bezwaar: wilt u dat uw kind niet meedoet, of wilt u gegevens laten verwijderen? Neem contact op met school. School kan de betreffende code direct laten verwijderen.

        Met vriendelijke groet,
        Schoolleiding
        """;
}
