using System.Security.Claims;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Infrastructure.WerkgeverApk;

public sealed partial class WerkgeverApkEmployeePortalService
{
    public async Task<(WerkgeverApkEmployeeQuestionDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetAbQuestionAsync(
        ClaimsPrincipal employee,
        string? itemId,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var catalog = CatalogFor(ctx.Code!.Team!.Company!);
        var state = ctx.State!;
        var item = WerkgeverApkAbProgress.ResolveNext(catalog, state, itemId);
        if (item is null)
        {
            return (null, new WerkgeverApkErrorDto("invalid_item", "Vraag niet gevonden."), 404);
        }

        var index = catalog.Items.ToList().FindIndex(i => i.Id == item.Id) + 1;
        var list = catalog.Items.ToList();
        var qIndex = list.FindIndex(i => i.Id == item.Id);
        string? previousItemId = qIndex > 0 ? list[qIndex - 1].Id : null;
        int? val = state.Answers.TryGetValue(item.Id, out var v) ? v : null;

        var dto = new WerkgeverApkEmployeeQuestionDto(
            item.Block,
            PupilMboHboProgressState.BlockKey(item.Block),
            item.Id,
            index,
            catalog.QuestionCount,
            item.DimensionA,
            item.Kind == WerkgeverApkAbItemKind.Check ? item.OptionA ?? item.Heading : item.Heading,
            item.Situation,
            PupilMboHboScaleLabels.ForBlock(item.Block),
            val,
            previousItemId,
            item.Kind,
            item.OptionA,
            item.OptionB,
            item.Heading,
            item.DisplayBlock,
            catalog.DisplayBlockCount,
            catalog.Package);

        return (dto, null, 200);
    }

    public async Task<(WerkgeverApkEmployeeAnswerResponse? Ok, WerkgeverApkErrorDto? Error, int Status)> SaveAbAnswerAsync(
        ClaimsPrincipal employee,
        string itemId,
        int value,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var (code, progress, state) = (ctx.Code!, ctx.Progress!, ctx.State!);
        var catalog = CatalogFor(code.Team!.Company!);
        var item = catalog.Find(itemId);
        if (item is null || !WerkgeverApkAnswerValidation.IsValidForItem(item, value))
        {
            return (null, new WerkgeverApkErrorDto("invalid_value", "Kies een antwoord."), 400);
        }

        state.Answers[itemId] = value;
        progress.ProgressJson = state.ToJson();
        progress.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;

        if (IsEmployeeCompleted(code, state, catalog))
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
        var list = catalog.Items.ToList();
        var idx = list.FindIndex(i => i.Id == itemId);

        var displayBlockDone = WerkgeverApkAbProgress.DisplayBlockComplete(state, catalog, item.DisplayBlock);
        if (displayBlockDone
            && catalog.Package == WerkgeverApkPackageKind.Uitgebreid
            && item.DisplayBlock < catalog.DisplayBlockCount
            && !WerkgeverApkAbProgress.AllAnswered(state, catalog))
        {
            nextPath = $"{WerkgeverApkEntryPaths.Pauze}?after={item.DisplayBlock}";
        }
        else if (idx >= 0 && idx < list.Count - 1)
        {
            nextItemId = list[idx + 1].Id;
        }
        else if (WerkgeverApkAbProgress.AllAnswered(state, catalog))
        {
            nextPath = WerkgeverApkEntryPaths.Delen;
        }

        var blockComplete = WerkgeverApkAbProgress.AllAnswered(state, catalog);
        return (new WerkgeverApkEmployeeAnswerResponse(nextItemId, nextPath, blockComplete), null, 200);
    }

    public async Task<(WerkgeverApkPauseDto? Ok, WerkgeverApkErrorDto? Error, int Status)> GetPauseAsync(
        ClaimsPrincipal employee,
        int afterDisplayBlock,
        CancellationToken ct = default)
    {
        var ctx = await ResolveEmployeeAsync(employee, ct);
        if (ctx.Error is not null)
        {
            return (null, ctx.Error, ctx.Status);
        }

        var catalog = CatalogFor(ctx.Code!.Team!.Company!);
        if (catalog.Package != WerkgeverApkPackageKind.Uitgebreid)
        {
            return (null, new WerkgeverApkErrorDto("not_applicable", "Pauze is alleen bij Uitgebreid."), 400);
        }

        var state = ctx.State!;
        var answered = WerkgeverApkAbProgress.AnsweredCount(state, catalog);
        return (new WerkgeverApkPauseDto(
            afterDisplayBlock,
            catalog.DisplayBlockCount,
            answered,
            catalog.QuestionCount,
            catalog.Package), null, 200);
    }

    private static WerkgeverApkEmployeeAbCatalog CatalogFor(Company company)
        => WerkgeverApkEmployeeAbCatalog.ForPackage(company.LoopbaanApkPackage);

    private static bool IsEmployeeCompleted(
        WerkgeverApkEmployeeCode code,
        PupilMboHboProgressState state,
        WerkgeverApkEmployeeAbCatalog catalog)
        => code.Status == WerkgeverApkEmployeeCodeStatus.Completed
           || WerkgeverApkAbProgress.AllAnswered(state, catalog);
}
