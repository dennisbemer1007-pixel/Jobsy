using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public interface IPupilMboHboPortalService
{
    Task<(PupilMboHboOverviewDto? Ok, PupilErrorDto? Error, int Status)> GetOverviewAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default);

    Task<(PupilMboHboIntroResponse? Ok, PupilErrorDto? Error, int Status)> MarkIntroSeenAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default);

    Task<(PupilMboHboQuestionDto? Ok, PupilErrorDto? Error, int Status)> GetQuestionAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        string? itemId,
        CancellationToken ct = default);

    Task<(PupilMboHboAnswerResponse? Ok, PupilErrorDto? Error, int Status)> SaveAnswerAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        string itemId,
        int value,
        CancellationToken ct = default);

    Task<(PupilMboHboAnswerResponse? Ok, PupilErrorDto? Error, int Status)> SaveVakAnswerAsync(
        ClaimsPrincipal pupil,
        string itemId,
        int leuk,
        int goed,
        CancellationToken ct = default);

    Task<(PupilMboHboOutcomeDto? Ok, PupilErrorDto? Error, int Status)> GetOutcomeAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        CancellationToken ct = default);

    Task<(PupilMboHboPassportDto? Ok, PupilErrorDto? Error, int Status)> GetPassportAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default);

    Task<TeacherMboHboClassProgressDto?> GetTeacherClassProgressAsync(
        ClaimsPrincipal staff,
        Guid classId,
        CancellationToken ct = default);
}

public sealed class PupilMboHboPortalService : IPupilMboHboPortalService
{
    private readonly JobsyDbContext _db;
    private readonly ISchoolScopeService _scope;
    private readonly IPupilCodeService _codes;
    private readonly IPupilMboHboResultBuilder _results;
    private readonly TimeProvider _clock;

    public PupilMboHboPortalService(
        JobsyDbContext db,
        ISchoolScopeService scope,
        IPupilCodeService codes,
        IPupilMboHboResultBuilder results,
        TimeProvider? clock = null)
    {
        _db = db;
        _scope = scope;
        _codes = codes;
        _results = results;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<(PupilMboHboOverviewDto? Ok, PupilErrorDto? Error, int Status)> GetOverviewAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default)
    {
        var ctx = await ResolvePupilAsync(pupil, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, schoolClass) = (ctx.Code!, ctx.Class!);
        var state = LoadState(code);
        var blocks = BuildBlockStatuses(state, schoolClass.Level);
        var completed = blocks.Count(b => b.Status == PupilMboHboTileStatus.Completed);
        var passport = state.AllBlocksComplete();

        return (new PupilMboHboOverviewDto(
            schoolClass.Name,
            pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value ?? "",
            state.IntroSeen,
            passport,
            completed,
            blocks), null, 200);
    }

    public async Task<(PupilMboHboIntroResponse? Ok, PupilErrorDto? Error, int Status)> MarkIntroSeenAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default)
    {
        var ctx = await ResolvePupilAsync(pupil, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var code = ctx.Code!;
        if (code.Status == PupilCodeStatus.NotStarted)
        {
            code.Status = PupilCodeStatus.InProgress;
        }

        var progress = await EnsureProgressAsync(code, ct);
        var state = LoadState(code);
        state.IntroSeen = true;
        progress.MboHboProgressJson = state.ToJson();
        progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(ct);

        return (new PupilMboHboIntroResponse(true, PupilMboHboEntryPaths.Overzicht), null, 200);
    }

    public async Task<(PupilMboHboQuestionDto? Ok, PupilErrorDto? Error, int Status)> GetQuestionAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        string? itemId,
        CancellationToken ct = default)
    {
        var ctx = await ResolvePupilAsync(pupil, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var schoolClass = ctx.Class!;
        var state = LoadState(ctx.Code!);
        var questions = PupilMboHboCatalog.Default.QuestionsFor(block, schoolClass.Level);
        if (questions.Count == 0)
        {
            return (null, new PupilErrorDto("invalid_block", "Onbekende test."), 400);
        }

        var q = ResolveQuestion(questions, state, block, itemId);
        if (q is null)
        {
            return (null, new PupilErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        var index = questions.ToList().FindIndex(x => x.Id == q.Id) + 1;
        int? val = null;
        int? leuk = null;
        int? goed = null;
        if (block == PupilMboHboBlock.Vakken)
        {
            if (state.VakScores.TryGetValue(q.Id, out var vs))
            {
                leuk = vs.Leuk;
                goed = vs.Goed;
            }
        }
        else if (state.Answers.TryGetValue(q.Id, out var v))
        {
            val = v;
        }

        return (new PupilMboHboQuestionDto(
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
            leuk,
            goed), null, 200);
    }

    public async Task<(PupilMboHboAnswerResponse? Ok, PupilErrorDto? Error, int Status)> SaveAnswerAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        if (!LikertAnswerJson.IsValidAnswer(value))
        {
            return (null, new PupilErrorDto("invalid_answer", "Kies een antwoord."), 400);
        }

        var ctx = await ResolvePupilAsync(pupil, ct, requireOpenWindow: true);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, schoolClass) = (ctx.Code!, ctx.Class!);
        var catalog = PupilMboHboCatalog.Default;
        if (catalog.Find(block, schoolClass.Level, itemId) is null)
        {
            return (null, new PupilErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        if (code.Status == PupilCodeStatus.NotStarted)
        {
            code.Status = PupilCodeStatus.InProgress;
        }

        var progress = await EnsureProgressAsync(code, ct);
        var state = LoadState(code);
        state.Answers[itemId] = value;
        await FinalizeBlockIfReady(code, progress, state, schoolClass, block, ct);

        var questions = catalog.QuestionsFor(block, schoolClass.Level);
        var next = NextUnanswered(block, questions, state);
        var idx = next is null ? questions.Count : questions.ToList().FindIndex(q => q.Id == next.Id) + 1;

        return (new PupilMboHboAnswerResponse(
            state.IsBlockComplete(block),
            state.AllBlocksComplete(),
            next?.Id,
            idx,
            questions.Count), null, 200);
    }

    public async Task<(PupilMboHboAnswerResponse? Ok, PupilErrorDto? Error, int Status)> SaveVakAnswerAsync(
        ClaimsPrincipal pupil,
        string itemId,
        int leuk,
        int goed,
        CancellationToken ct = default)
    {
        if (!LikertAnswerJson.IsValidAnswer(leuk) || !LikertAnswerJson.IsValidAnswer(goed))
        {
            return (null, new PupilErrorDto("invalid_answer", "Kies een antwoord."), 400);
        }

        var ctx = await ResolvePupilAsync(pupil, ct, requireOpenWindow: true);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, schoolClass) = (ctx.Code!, ctx.Class!);
        if (PupilMboHboCatalog.Default.Find(PupilMboHboBlock.Vakken, schoolClass.Level, itemId) is null)
        {
            return (null, new PupilErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        if (code.Status == PupilCodeStatus.NotStarted)
        {
            code.Status = PupilCodeStatus.InProgress;
        }

        var progress = await EnsureProgressAsync(code, ct);
        var state = LoadState(code);
        state.VakScores[itemId] = new PupilMboHboVakScore { Leuk = leuk, Goed = goed };
        await FinalizeBlockIfReady(code, progress, state, schoolClass, PupilMboHboBlock.Vakken, ct);

        var questions = PupilMboHboCatalog.Default.QuestionsFor(PupilMboHboBlock.Vakken, schoolClass.Level);
        var next = NextUnanswered(PupilMboHboBlock.Vakken, questions, state);
        var idx = next is null ? questions.Count : questions.ToList().FindIndex(q => q.Id == next.Id) + 1;

        return (new PupilMboHboAnswerResponse(
            state.IsBlockComplete(PupilMboHboBlock.Vakken),
            state.AllBlocksComplete(),
            next?.Id,
            idx,
            questions.Count), null, 200);
    }

    public async Task<(PupilMboHboOutcomeDto? Ok, PupilErrorDto? Error, int Status)> GetOutcomeAsync(
        ClaimsPrincipal pupil,
        PupilMboHboBlock block,
        CancellationToken ct = default)
    {
        var ctx = await ResolvePupilAsync(pupil, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var schoolClass = ctx.Class!;
        var state = LoadState(ctx.Code!);
        if (!state.IsBlockComplete(block))
        {
            return (null, new PupilErrorDto("block_open", "Deze test is nog niet af."), 409);
        }

        var outcome = PupilMboHboScoring.BuildOutcome(block, state, schoolClass.Level);
        var meta = PupilMboHboCopy.BlockMeta(block);
        var answered = state.AnsweredInBlock(block, PupilMboHboCatalog.Default, schoolClass.Level);

        return (new PupilMboHboOutcomeDto(
            block,
            PupilMboHboProgressState.BlockKey(block),
            meta.Title,
            answered,
            meta.QuestionCount,
            outcome.Bars.Select(b => new PupilMboHboBarDto(b.Label, b.Percent)).ToList(),
            outcome.Summary,
            outcome.Highlights), null, 200);
    }

    public async Task<(PupilMboHboPassportDto? Ok, PupilErrorDto? Error, int Status)> GetPassportAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct = default)
    {
        var ctx = await ResolvePupilAsync(pupil, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, schoolClass) = (ctx.Code!, ctx.Class!);
        var state = LoadState(code);
        if (!state.AllBlocksComplete())
        {
            return (null, new PupilErrorDto("locked", "Je paspoort komt vrij als alle 5 testen af zijn."), 409);
        }

        await _results.BuildAsync(code.Id, ct);
        var schoolName = await _db.Schools.AsNoTracking()
            .Where(s => s.Id == schoolClass.SchoolId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var sections = new List<PupilMboHboPassportSectionDto>();
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            var o = PupilMboHboScoring.BuildOutcome(block, state, schoolClass.Level);
            var meta = PupilMboHboCopy.BlockMeta(block);
            var body = $"<p>{System.Net.WebUtility.HtmlEncode(o.Summary)}</p>";
            if (o.Highlights.Count > 0)
            {
                body += "<p>" + string.Join(" · ", o.Highlights.Select(System.Net.WebUtility.HtmlEncode)) + "</p>";
            }

            sections.Add(new PupilMboHboPassportSectionDto(meta.Title, body));
        }

        return (new PupilMboHboPassportDto(
            schoolClass.Name,
            pupil.FindFirst(PupilClaimTypes.CodeDisplay)?.Value ?? "",
            schoolName,
            true,
            sections,
            "Dit paspoort laat zien wat nu mogelijk bij je past. Het is geen advies en geen eindoordeel."), null, 200);
    }

    public async Task<TeacherMboHboClassProgressDto?> GetTeacherClassProgressAsync(
        ClaimsPrincipal staff,
        Guid classId,
        CancellationToken ct = default)
    {
        if (!await _scope.CanTeachClassAsync(staff, classId, ct))
        {
            return null;
        }

        var schoolClass = await _db.SchoolClasses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId, ct);
        if (schoolClass is null || schoolClass.QuestionSet != PupilQuestionSet.MboHbo)
        {
            return null;
        }

        var codes = await _db.PupilCodes.AsNoTracking()
            .Include(c => c.Progress)
            .Where(c => c.SchoolClassId == classId)
            .OrderBy(c => c.Number)
            .ToListAsync(ct);

        var rows = new List<TeacherMboHboCodeRowDto>();
        foreach (var code in codes)
        {
            var state = PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson);
            var statuses = PupilMboHboCatalog.AllBlocks
                .Select(b => TileStatus(state, b, schoolClass.Level))
                .ToList();
            var done = statuses.Count(s => s == PupilMboHboTileStatus.Completed);
            var pattern = PupilMboHboCatalog.AllBlocks.Any(b =>
                state.IsBlockComplete(b)
                && PupilMboHboScoring.BlockHasPatternSignal(b, state, schoolClass.Level));
            var display = _codes.Unprotect(code.CodeProtected) is { } raw
                ? PupilCodeFormat.Display(raw)
                : "—";
            rows.Add(new TeacherMboHboCodeRowDto(
                code.Id,
                display,
                statuses,
                done,
                done >= 5,
                pattern));
        }

        var charts = BuildGroupCharts(codes, schoolClass.Level);
        return new TeacherMboHboClassProgressDto(
            classId,
            schoolClass.Name,
            PupilQuestionSet.MboHbo,
            rows,
            charts);
    }

    private static TeacherMboHboGroupChartsDto BuildGroupCharts(
        IReadOnlyList<PupilCode> codes,
        SchoolLevel level)
    {
        var completed = codes
            .Select(c => PupilMboHboProgressState.Parse(c.Progress?.MboHboProgressJson))
            .Where(s => s.IsBlockComplete(PupilMboHboBlock.Interesse))
            .ToList();

        var interesseBars = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var state in completed)
        {
            var outcome = PupilMboHboScoring.BuildOutcome(
                PupilMboHboBlock.Interesse,
                state,
                level);
            foreach (var bar in outcome.Bars)
            {
                if (!interesseBars.TryGetValue(bar.Label, out var list))
                {
                    list = [];
                    interesseBars[bar.Label] = list;
                }

                list.Add(bar.Percent);
            }
        }

        var avgInteresse = interesseBars
            .Select(kv => new PupilMboHboBarDto(
                kv.Key,
                kv.Value.Count == 0 ? 0 : (int)Math.Round(kv.Value.Average())))
            .OrderByDescending(b => b.Percent)
            .ToList();

        var blockCompletion = PupilMboHboCatalog.AllBlocks.Select(block =>
        {
            var key = PupilMboHboProgressState.BlockKey(block);
            var total = codes.Count;
            if (total == 0)
            {
                return new TeacherMboHboBlockCompletionDto(key, 0, 0);
            }

            var done = 0;
            var busy = 0;
            foreach (var code in codes)
            {
                var state = PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson);
                var st = TileStatus(state, block, level);
                if (st == PupilMboHboTileStatus.Completed)
                {
                    done++;
                }
                else if (st == PupilMboHboTileStatus.InProgress)
                {
                    busy++;
                }
            }

            return new TeacherMboHboBlockCompletionDto(
                key,
                (int)Math.Round(100d * done / total),
                (int)Math.Round(100d * busy / total));
        }).ToList();

        return new TeacherMboHboGroupChartsDto(avgInteresse, blockCompletion);
    }

    private static PupilMboHboTileStatus TileStatus(
        PupilMboHboProgressState state,
        PupilMboHboBlock block,
        SchoolLevel level)
    {
        if (state.IsBlockComplete(block))
        {
            return PupilMboHboTileStatus.Completed;
        }

        var answered = state.AnsweredInBlock(block, PupilMboHboCatalog.Default, level);
        return answered > 0 ? PupilMboHboTileStatus.InProgress : PupilMboHboTileStatus.NotStarted;
    }

    private static IReadOnlyList<PupilMboHboBlockStatusDto> BuildBlockStatuses(
        PupilMboHboProgressState state,
        SchoolLevel level)
    {
        var list = new List<PupilMboHboBlockStatusDto>();
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            var meta = PupilMboHboCopy.BlockMeta(block);
            var answered = state.AnsweredInBlock(block, PupilMboHboCatalog.Default, level);
            var status = TileStatus(state, block, level);
            list.Add(new PupilMboHboBlockStatusDto(
                block,
                PupilMboHboProgressState.BlockKey(block),
                meta.Title,
                meta.Description,
                meta.QuestionCount,
                answered,
                status));
        }

        return list;
    }

    private async Task FinalizeBlockIfReady(
        PupilCode code,
        PupilProgress progress,
        PupilMboHboProgressState state,
        SchoolClass schoolClass,
        PupilMboHboBlock block,
        CancellationToken ct)
    {
        progress.MboHboProgressJson = state.ToJson();
        progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;

        if (state.BlockFullyAnswered(block, PupilMboHboCatalog.Default, schoolClass.Level)
            && !state.IsBlockComplete(block))
        {
            state.BlocksCompleted[PupilMboHboProgressState.BlockKey(block)] =
                _clock.GetUtcNow().UtcDateTime.ToString("O");
            progress.MboHboProgressJson = state.ToJson();
        }

        if (state.AllBlocksComplete())
        {
            progress.CompletedAtUtc = _clock.GetUtcNow().UtcDateTime;
            code.Status = PupilCodeStatus.Completed;
            await _db.SaveChangesAsync(ct);
            await _results.BuildAsync(code.Id, ct);
        }
        else
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    private static PupilMboHboQuestion? ResolveQuestion(
        IReadOnlyList<PupilMboHboQuestion> questions,
        PupilMboHboProgressState state,
        PupilMboHboBlock block,
        string? itemId)
    {
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return questions.FirstOrDefault(q => q.Id == itemId);
        }

        var next = NextUnanswered(block, questions, state);
        return next ?? (questions.Count > 0 ? questions[0] : null);
    }

    private static PupilMboHboQuestion? NextUnanswered(
        PupilMboHboBlock block,
        IReadOnlyList<PupilMboHboQuestion> questions,
        PupilMboHboProgressState state)
    {
        foreach (var q in questions)
        {
            if (block == PupilMboHboBlock.Vakken)
            {
                if (!state.VakScores.TryGetValue(q.Id, out var s)
                    || !LikertAnswerJson.IsValidAnswer(s.Leuk)
                    || !LikertAnswerJson.IsValidAnswer(s.Goed))
                {
                    return q;
                }
            }
            else if (!state.Answers.ContainsKey(q.Id))
            {
                return q;
            }
        }

        return null;
    }

    private static PupilMboHboProgressState LoadState(PupilCode code)
        => PupilMboHboProgressState.Parse(code.Progress?.MboHboProgressJson);

    private async Task<PupilProgress> EnsureProgressAsync(PupilCode code, CancellationToken ct)
    {
        if (code.Progress is not null)
        {
            return code.Progress;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var progress = new PupilProgress
        {
            PupilCodeId = code.Id,
            StartedAtUtc = now,
            UpdatedAtUtc = now,
            MboHboProgressJson = new PupilMboHboProgressState().ToJson()
        };
        _db.PupilProgresses.Add(progress);
        code.Progress = progress;
        await _db.SaveChangesAsync(ct);
        return progress;
    }

    private async Task<(PupilCode? Code, SchoolClass? Class, PupilErrorDto? Error, int Status)> ResolvePupilAsync(
        ClaimsPrincipal pupil,
        CancellationToken ct,
        bool requireOpenWindow = false)
    {
        if (!Guid.TryParse(pupil.FindFirst(PupilClaimTypes.PupilCodeId)?.Value, out var codeId))
        {
            return (null, null, new PupilErrorDto(PupilPortalService.SessionInvalidError, "Sessie verlopen."), 401);
        }

        var code = await _db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.SchoolClass)
            .FirstOrDefaultAsync(c => c.Id == codeId, ct);
        if (code?.SchoolClass is null)
        {
            return (null, null, new PupilErrorDto(PupilPortalService.SessionInvalidError, "Sessie verlopen."), 401);
        }

        if (code.SchoolClass.QuestionSet != PupilQuestionSet.MboHbo)
        {
            return (null, null, new PupilErrorDto("wrong_set", "Deze route is voor MBO/HBO."), 409);
        }

        if (requireOpenWindow && code.SchoolClass.TestWindow != TestWindowState.Open
            && code.Status != PupilCodeStatus.Completed)
        {
            return (null, null, new PupilErrorDto(PupilPortalService.WindowClosedError, "Het testvenster is gesloten."), 409);
        }

        return (code, code.SchoolClass, null, 200);
    }
}
