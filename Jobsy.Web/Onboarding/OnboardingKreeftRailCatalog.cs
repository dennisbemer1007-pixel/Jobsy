using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Zone rail (mockup 04–14) titles and step-aware subtitles.</summary>
public static class OnboardingKreeftRailCatalog
{
    public sealed record RailZone(string TitleKey, string DefaultSubKey, bool IsDone, bool IsCurrent, string? ActiveSub);

    private static readonly (string TitleKey, string DefaultSubKey)[] Zones =
    [
        ("Onboarding.Kreeft.Rail.Strand", "Onboarding.Kreeft.Rail.StrandSub"),
        ("Onboarding.Kreeft.Rail.Kust", "Onboarding.Kreeft.Rail.KustSubIdle"),
        ("Onboarding.Kreeft.Rail.Rotsen", "Onboarding.Kreeft.Rail.RotsenSubIdle"),
        ("Onboarding.Kreeft.Rail.Diepte", "Onboarding.Kreeft.Rail.DiepteSubIdle"),
        ("Onboarding.Kreeft.Rail.Licht", "Onboarding.Kreeft.Rail.LichtSubIdle"),
    ];

    public static IReadOnlyList<RailZone> ForOnboardingStep(int step, CultureState culture)
    {
        var state = KreeftReiskaartCatalog.ForOnboardingStep(step);
        var activeZone = state.ZoneIndex;
        var items = new RailZone[Zones.Length];
        for (var zi = 0; zi < Zones.Length; zi++)
        {
            var (titleKey, idleSubKey) = Zones[zi];
            var isDone = zi < activeZone;
            var isCurrent = zi == activeZone;
            string? activeSub = null;
            if (isCurrent)
            {
                activeSub = culture.Format("Onboarding.Kreeft.Rail.StepLine", step, culture[$"Onboarding.Step{step}.Title"]);
            }

            items[zi] = new RailZone(titleKey, idleSubKey, isDone, isCurrent, activeSub);
        }

        return items;
    }

    public static (string Primary, string Secondary) ZoneTag(int step, CultureState culture)
    {
        var state = KreeftReiskaartCatalog.ForOnboardingStep(step);
        var primary = culture[KreeftReiskaartCatalog.ZoneNameKeys[state.ZoneIndex]];
        var kop = culture[state.KopKey];
        var kopTail = kop.Contains('·', StringComparison.Ordinal)
            ? kop.Split('·', 2)[1].Trim()
            : kop.Trim();
        var secondary = $"{kopTail} · {culture[state.SubKey]}";
        return (primary, secondary);
    }
}
