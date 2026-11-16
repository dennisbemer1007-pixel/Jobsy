using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkAbProgress
{
    public static bool AllAnswered(PupilMboHboProgressState state, WerkgeverApkEmployeeAbCatalog catalog)
    {
        foreach (var item in catalog.Items)
        {
            if (!state.Answers.TryGetValue(item.Id, out var v)
                || !WerkgeverApkAnswerValidation.IsValidForItem(item, v))
            {
                return false;
            }
        }

        return catalog.Items.Count > 0;
    }

    public static bool DisplayBlockComplete(
        PupilMboHboProgressState state,
        WerkgeverApkEmployeeAbCatalog catalog,
        int displayBlock)
    {
        var items = catalog.ItemsInDisplayBlock(displayBlock);
        if (items.Count == 0)
        {
            return false;
        }

        foreach (var item in items)
        {
            if (!state.Answers.TryGetValue(item.Id, out var v)
                || !WerkgeverApkAnswerValidation.IsValidForItem(item, v))
            {
                return false;
            }
        }

        return true;
    }

    public static WerkgeverApkAbItem? ResolveNext(
        WerkgeverApkEmployeeAbCatalog catalog,
        PupilMboHboProgressState state,
        string? itemId)
    {
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return catalog.Find(itemId);
        }

        foreach (var item in catalog.Items)
        {
            if (!state.Answers.ContainsKey(item.Id))
            {
                return item;
            }
        }

        return catalog.Items.Count > 0 ? catalog.Items[0] : null;
    }

    public static int AnsweredCount(PupilMboHboProgressState state, WerkgeverApkEmployeeAbCatalog catalog)
    {
        var n = 0;
        foreach (var item in catalog.Items)
        {
            if (state.Answers.TryGetValue(item.Id, out var v)
                && WerkgeverApkAnswerValidation.IsValidForItem(item, v))
            {
                n++;
            }
        }

        return n;
    }
}
