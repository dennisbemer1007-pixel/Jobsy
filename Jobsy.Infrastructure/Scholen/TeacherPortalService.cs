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

namespace Jobsy.Infrastructure.Scholen;

public interface ITeacherPortalService
{
    Task<IReadOnlyList<TeacherAssignedClassDto>> ListAssignedClassesAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<TeacherClassOverviewDto?> GetOverviewAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherCodeRowDto>?> ListCodesAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<TeacherGroupInsightsDto?> GetGroupAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<TeacherDreamJobsDto?> GetDreamJobsAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<TeacherCodeDetailDto?> GetCodeDetailAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalCodeRowDto? Row, string? Error)> ReplaceCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetTestWindowAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        DateOnly? closesOn,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListPdfAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);

    Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListCsvAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default);
}

public sealed class TeacherPortalService : ITeacherPortalService
{
    public const int ProgressTotalQuestions = SchoolPortalService.ProgressTotalQuestions;
    public const int CodesPreviewLimit = 10;

    private readonly JobsyDbContext _db;
    private readonly ISchoolScopeService _scope;
    private readonly IPupilCodeService _codes;
    private readonly ISchoolPortalService _schoolPortal;
    private readonly IPupilStoryRenderer _story;
    private readonly IPersonalDataAccessLogger _accessLog;

    public TeacherPortalService(
        JobsyDbContext db,
        ISchoolScopeService scope,
        IPupilCodeService codes,
        ISchoolPortalService schoolPortal,
        IPupilStoryRenderer story,
        IPersonalDataAccessLogger accessLog)
    {
        _db = db;
        _scope = scope;
        _codes = codes;
        _schoolPortal = schoolPortal;
        _story = story;
        _accessLog = accessLog;
    }

    public async Task<IReadOnlyList<TeacherAssignedClassDto>> ListAssignedClassesAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return [];
        }

        var year = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow));
        var classIds = await _db.TeacherClassAssignments.AsNoTracking()
            .Where(a => a.TeacherUserId == userId)
            .Select(a => a.SchoolClassId)
            .ToListAsync(cancellationToken);

        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => classIds.Contains(c.Id) && c.SchoolYearStart == year)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return classes
            .Select(c => new TeacherAssignedClassDto(
                c.Id,
                c.Name,
                c.Level,
                c.Year,
                c.SchoolYearStart,
                SchoolYear.Label(c.SchoolYearStart)))
            .ToList();
    }

    public async Task<TeacherClassOverviewDto?> GetOverviewAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var schoolId = await SchoolIdOfClassAsync(classId, cancellationToken);
        if (schoolId is Guid sid)
        {
            await _schoolPortal.EnsureTestWindowsClosedAsync(sid, cancellationToken);
        }

        var schoolClass = await _db.SchoolClasses.AsNoTracking()
            .Include(c => c.PupilCodes)
            .ThenInclude(c => c.Progress)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (schoolClass is null)
        {
            return null;
        }

        var codes = schoolClass.PupilCodes.OrderBy(c => c.Number).ToList();
        var completed = codes.Count(c => c.Status == PupilCodeStatus.Completed);
        var inProgress = codes.Count(c => c.Status == PupilCodeStatus.InProgress);
        var notStarted = codes.Count(c => c.Status == PupilCodeStatus.NotStarted);
        var total = codes.Count;
        var pct = total == 0 ? 0d : Math.Round(100d * completed / total, 1);

        int? avgMinutes = null;
        if (completed >= SchoolAnonymity.MinGroupSize)
        {
            var durations = codes
                .Where(c => c.Status == PupilCodeStatus.Completed && c.Progress?.StartedAtUtc is not null
                            && c.Progress.CompletedAtUtc is not null)
                .Select(c => (c.Progress!.CompletedAtUtc!.Value - c.Progress.StartedAtUtc).TotalMinutes)
                .Where(m => m > 0 && m < 24 * 60)
                .ToList();
            if (durations.Count >= SchoolAnonymity.MinGroupSize)
            {
                avgMinutes = (int)Math.Round(durations.Average());
            }
        }

        var results = await _db.PupilResults.AsNoTracking()
            .Where(r => r.SchoolClassId == classId)
            .ToListAsync(cancellationToken);
        var group = MapGroupInsights(results);

        var preview = codes.Take(CodesPreviewLimit).Select(MapCodeRow).ToList();

        return new TeacherClassOverviewDto(
            ClassId: schoolClass.Id,
            ClassName: schoolClass.Name,
            Level: schoolClass.Level,
            Year: schoolClass.Year,
            SchoolYearLabel: SchoolYear.Label(schoolClass.SchoolYearStart),
            CodeCount: total,
            CompletedCount: completed,
            InProgressCount: inProgress,
            NotStartedCount: notStarted,
            CompletedPercent: pct,
            AverageMinutes: avgMinutes,
            TestWindow: schoolClass.TestWindow,
            TestWindowClosesOn: schoolClass.TestWindowClosesOn,
            ParentalInfoConfirmed: schoolClass.ParentalInfoConfirmedAtUtc is not null,
            CodesPreview: preview,
            GroupInsights: group);
    }

    public async Task<IReadOnlyList<TeacherCodeRowDto>?> ListCodesAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var codes = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.Progress)
            .Where(c => c.SchoolClassId == classId)
            .OrderBy(c => c.Number)
            .ToListAsync(cancellationToken);

        return codes.Select(MapCodeRow).ToList();
    }

    public async Task<TeacherGroupInsightsDto?> GetGroupAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var results = await _db.PupilResults.AsNoTracking()
            .Where(r => r.SchoolClassId == classId)
            .ToListAsync(cancellationToken);
        return MapGroupInsights(results);
    }

    public async Task<TeacherDreamJobsDto?> GetDreamJobsAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var results = await _db.PupilResults.AsNoTracking()
            .Where(r => r.SchoolClassId == classId)
            .ToListAsync(cancellationToken);
        var agg = ClassResultsAggregator.AggregateTeacherGroup(results);
        return new TeacherDreamJobsDto(
            Visible: agg.Visible,
            CompletedCount: agg.CompletedCount,
            Jobs: agg.DreamJobs.Select(d => new NamedCountDto(d.Key, d.Count)).ToList(),
            UndecidedCount: agg.UndecidedDreamJobCount);
    }

    public async Task<TeacherCodeDetailDto?> GetCodeDetailAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default)
    {
        // D2 + §R: detail only when CanSeePerCodeDetail (assignment). Setting D4 does not apply.
        if (!await _scope.CanSeePerCodeDetailAsync(user, classId, cancellationToken))
        {
            return null;
        }

        var code = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .Include(c => c.SchoolClass)
            .FirstOrDefaultAsync(c => c.Id == codeId && c.SchoolClassId == classId, cancellationToken);
        if (code?.SchoolClass is null)
        {
            return null;
        }

        var display = _codes.Unprotect(code.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";

        var progressCurrent = code.Status switch
        {
            PupilCodeStatus.Completed => ProgressTotalQuestions,
            PupilCodeStatus.InProgress => Math.Clamp(code.Progress?.CurrentIndex ?? 0, 0, ProgressTotalQuestions),
            _ => 0
        };

        int? duration = null;
        if (code.Progress?.StartedAtUtc is DateTime started && code.Progress.CompletedAtUtc is DateTime done)
        {
            duration = (int)Math.Round((done - started).TotalMinutes);
        }

        PupilStoryViewDto? story = null;
        DreamJobRouteStubDto? dream = null;
        IReadOnlyList<string> starters = [];
        IReadOnlyList<string> likes = [];
        IReadOnlyList<string> dislikes = [];
        string? likeOther = null;
        string? dislikeOther = null;

        if (code.Status == PupilCodeStatus.Completed && code.Result is not null)
        {
            story = _story.Render(code.Result, code.Progress);
            dream = EnrichDreamTitle(_story.RenderDreamRoute(code.Result, code.Progress));
            starters = _story.ConversationStarterKeys(code.Result);
            likes = ParseChipList(code.Progress?.LikesJson);
            dislikes = ParseChipList(code.Progress?.DislikesJson);
            likeOther = code.Progress?.LikeOtherWord;
            dislikeOther = code.Progress?.DislikeOtherWord;

            await LogPupilCodeViewAsync(user, codeId, cancellationToken);
        }

        return new TeacherCodeDetailDto(
            CodeId: code.Id,
            ClassId: classId,
            DisplayCode: display,
            ClassName: code.SchoolClass.Name,
            Status: code.Status,
            ProgressCurrent: progressCurrent,
            ProgressTotal: ProgressTotalQuestions,
            CompletedAtUtc: code.Result?.CompletedAtUtc ?? code.Progress?.CompletedAtUtc,
            DurationMinutes: duration,
            Story: story,
            Likes: likes,
            Dislikes: dislikes,
            LikeOtherWord: likeOther,
            DislikeOtherWord: dislikeOther,
            ConversationStarterKeys: starters,
            DreamJob: dream,
            PdfAvailable: false);
    }

    public Task<(SchoolPortalCodeRowDto? Row, string? Error)> ReplaceCodeAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken = default)
        => ReplaceCodeAsTeacherAsync(user, classId, codeId, cancellationToken);

    public async Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetTestWindowAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        DateOnly? closesOn,
        CancellationToken cancellationToken = default)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
        {
            return (null, "not_found", "not_found");
        }

        return await SetTestWindowInternalAsync(user, classId, action, closesOn, cancellationToken);
    }

    public Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListPdfAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
        => _schoolPortal.BuildCodeListPdfAsync(user, classId, cancellationToken);

    public Task<(byte[]? Bytes, string? FileName, string? Error)> BuildCodeListCsvAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
        => _schoolPortal.BuildCodeListCsvAsync(user, classId, cancellationToken);

    private async Task<(SchoolPortalCodeRowDto? Row, string? Error)> ReplaceCodeAsTeacherAsync(
        ClaimsPrincipal user,
        Guid classId,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        if (!await _scope.CanTeachClassAsync(user, classId, cancellationToken))
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
        var display = _codes.Unprotect(code.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";
        var progressCurrent = code.Status switch
        {
            PupilCodeStatus.Completed => ProgressTotalQuestions,
            PupilCodeStatus.InProgress => Math.Clamp(code.Progress?.CurrentIndex ?? 0, 0, ProgressTotalQuestions),
            _ => 0
        };
        return (new SchoolPortalCodeRowDto(
            code.Id,
            code.Number,
            display,
            code.Status,
            progressCurrent,
            ProgressTotalQuestions,
            code.LastSeenAtUtc), null);
    }

    private async Task<(SchoolPortalClassDetailDto? Detail, string? Error, string? ErrorCode)> SetTestWindowInternalAsync(
        ClaimsPrincipal user,
        Guid classId,
        string action,
        DateOnly? closesOn,
        CancellationToken cancellationToken)
    {
        // Reuse school portal rules (409 parental / processor) by temporarily allowing teach scope.
        // SchoolPortalService.SetTestWindowAsync only allows CanManageClass — call shared logic via DB here.
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
                    "De schoolbeheerder moet eerst bevestigen dat ouders zijn geïnformeerd.",
                    "parental_info_missing");
            }

            entity.TestWindow = TestWindowState.Open;
            entity.TestWindowClosesOn = closesOn;
        }
        else if (act == "close")
        {
            entity.TestWindow = TestWindowState.Closed;
            entity.TestWindowClosesOn = null;
        }
        else
        {
            return (null, "Onbekende actie.", "validation");
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Map a minimal detail for the teacher UI (codes not required on test-window response).
        var codes = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.Progress)
            .Where(c => c.SchoolClassId == classId)
            .OrderBy(c => c.Number)
            .ToListAsync(cancellationToken);

        var dto = new SchoolPortalClassDetailDto(
            entity.Id,
            entity.Name,
            entity.Level,
            entity.Year,
            entity.SchoolYearStart,
            SchoolYear.Label(entity.SchoolYearStart),
            entity.PupilCount,
            [],
            entity.ParentalInfoConfirmedAtUtc is not null,
            entity.ParentalInfoConfirmedAtUtc,
            null,
            entity.ParentalInfoTextVersion,
            entity.TestWindow,
            entity.TestWindowClosesOn,
            entity.School?.ProcessorAgreementSignedOn is not null,
            codes.Select(c =>
            {
                var display = _codes.Unprotect(c.CodeProtected) is { } raw
                    ? PupilCodeFormat.Display(raw)
                    : "******";
                var progressCurrent = c.Status switch
                {
                    PupilCodeStatus.Completed => ProgressTotalQuestions,
                    PupilCodeStatus.InProgress => Math.Clamp(c.Progress?.CurrentIndex ?? 0, 0, ProgressTotalQuestions),
                    _ => 0
                };
                return new SchoolPortalCodeRowDto(
                    c.Id, c.Number, display, c.Status, progressCurrent, ProgressTotalQuestions, c.LastSeenAtUtc);
            }).ToList());

        return (dto, null, null);
    }

    private TeacherGroupInsightsDto MapGroupInsights(IReadOnlyList<PupilResult> results)
    {
        var agg = ClassResultsAggregator.AggregateTeacherGroup(results);
        var prompts = agg.Visible ? _story.ClassDiscussionPromptKeys() : Array.Empty<string>();
        return new TeacherGroupInsightsDto(
            Visible: agg.Visible,
            CompletedCount: agg.CompletedCount,
            RiasecBars: agg.RiasecBars
                .Select(b => new RiasecBarDto(b.Key, RiasecKidLabels.LabelKey(b.Key), b.Count))
                .ToList(),
            TopValues: agg.TopValues.Select(v => new NamedCountDto(v.Key, v.Count)).ToList(),
            DreamJobs: agg.DreamJobs.Select(d => new NamedCountDto(d.Key, d.Count)).ToList(),
            TopCultures: agg.TopCultures.Select(c => new NamedCountDto(c.Key, c.Count)).ToList(),
            CompetenceBands: agg.CompetenceBands.Select(c => new NamedCountDto(c.Key, c.Count)).ToList(),
            DiscussionPromptKeys: prompts);
    }

    private TeacherCodeRowDto MapCodeRow(PupilCode code)
    {
        var display = _codes.Unprotect(code.CodeProtected) is { } raw
            ? PupilCodeFormat.Display(raw)
            : "******";
        var progressCurrent = code.Status switch
        {
            PupilCodeStatus.Completed => ProgressTotalQuestions,
            PupilCodeStatus.InProgress => Math.Clamp(code.Progress?.CurrentIndex ?? 0, 0, ProgressTotalQuestions),
            _ => 0
        };
        return new TeacherCodeRowDto(
            code.Id,
            code.Number,
            display,
            code.Status,
            progressCurrent,
            ProgressTotalQuestions,
            code.LastSeenAtUtc);
    }

    private static DreamJobRouteStubDto EnrichDreamTitle(DreamJobRouteStubDto stub)
    {
        if (string.IsNullOrWhiteSpace(stub.JobKey))
        {
            return stub with { JobTitle = null };
        }

        var job = DreamJobCatalog.All.FirstOrDefault(j =>
            string.Equals(j.Key, stub.JobKey, StringComparison.OrdinalIgnoreCase));
        return stub with { JobTitle = job?.TitleNl ?? stub.JobKey };
    }

    private static IReadOnlyList<string> ParseChipList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task LogPupilCodeViewAsync(
        ClaimsPrincipal user,
        Guid codeId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user) ?? Guid.Empty;
        var role = RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
            ? JobsyRoles.SchoolAdmin
            : JobsyRoles.Teacher;
        await _accessLog.LogAsync(new PersonalDataAccessEntry(
            userId,
            role,
            "school.pupil-code",
            "view",
            SubjectPupilCodeId: codeId), cancellationToken);
    }

    private async Task<Guid?> SchoolIdOfClassAsync(Guid classId, CancellationToken cancellationToken)
        => await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => (Guid?)c.SchoolId)
            .FirstOrDefaultAsync(cancellationToken);

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
