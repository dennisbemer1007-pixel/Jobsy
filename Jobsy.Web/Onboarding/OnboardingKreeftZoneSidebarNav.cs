namespace Jobsy.Web.Onboarding;

/// <summary>Five journey zones (Strand → Licht) for kreeft mockup side nav on welcome + steps 1–10.</summary>
public static class OnboardingKreeftZoneSidebarNav
{
    public sealed record ZoneItem(string LabelKey, bool IsCurrent, bool IsDone);

    public static IReadOnlyList<ZoneItem> ForWelcome() => ForActiveZoneIndex(0);

    public static IReadOnlyList<ZoneItem> ForOnboardingStep(int step)
    {
        var state = KreeftReiskaartCatalog.ForOnboardingStep(step);
        return ForActiveZoneIndex(state.ZoneIndex);
    }

    private static IReadOnlyList<ZoneItem> ForActiveZoneIndex(int activeZoneIndex)
    {
        var keys = KreeftReiskaartCatalog.ZoneNameKeys;
        var items = new ZoneItem[keys.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            items[i] = new ZoneItem(keys[i], i == activeZoneIndex, i < activeZoneIndex);
        }

        return items;
    }
}
