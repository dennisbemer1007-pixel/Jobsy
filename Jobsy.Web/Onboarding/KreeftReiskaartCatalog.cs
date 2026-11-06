namespace Jobsy.Web.Onboarding;

/// <summary>
/// Reiskaart state for kreeft journey screens 1–16 (mockup copydeck / gen.py).
/// </summary>
internal static class KreeftReiskaartCatalog
{
    internal readonly record struct ScreenState(
        int ScreenNumber,
        int ZoneIndex,
        int Shed,
        bool NewShell,
        bool MidDepthPause,
        string KopKey,
        string SubKey,
        string? TipKey);

    internal static int ScreenForOnboardingStep(int step) =>
        step switch
        {
            >= 1 and <= 8 => step + 3,
            9 => 13,
            10 => 14,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, "Onboarding step must be 1–10."),
        };

    internal static ScreenState ForScreen(int screenNumber) =>
        screenNumber switch
        {
            1 => new(1, 0, 0, false, false, "Reiskaart.S01.Kop", "Reiskaart.S01.Sub", null),
            2 => new(2, 0, 0, false, false, "Reiskaart.S02.Kop", "Reiskaart.S02.Sub", "Reiskaart.S02.Tip"),
            3 => new(3, 0, 0, false, false, "Reiskaart.S03.Kop", "Reiskaart.S03.Sub", "Reiskaart.S03.Tip"),
            4 => new(4, 1, 0, false, false, "Reiskaart.S04.Kop", "Reiskaart.S04.Sub", "Reiskaart.S04.Tip"),
            5 => new(5, 1, 1, false, false, "Reiskaart.S05.Kop", "Reiskaart.S05.Sub", "Reiskaart.S05.Tip"),
            6 => new(6, 1, 2, false, false, "Reiskaart.S06.Kop", "Reiskaart.S06.Sub", "Reiskaart.S06.Tip"),
            7 => new(7, 2, 3, false, false, "Reiskaart.S07.Kop", "Reiskaart.S07.Sub", "Reiskaart.S07.Tip"),
            8 => new(8, 2, 4, false, false, "Reiskaart.S08.Kop", "Reiskaart.S08.Sub", "Reiskaart.S08.Tip"),
            9 => new(9, 2, 5, false, false, "Reiskaart.S09.Kop", "Reiskaart.S09.Sub", "Reiskaart.S09.Tip"),
            10 => new(10, 3, 6, false, false, "Reiskaart.S10.Kop", "Reiskaart.S10.Sub", "Reiskaart.S10.Tip"),
            11 => new(11, 3, 7, false, false, "Reiskaart.S11.Kop", "Reiskaart.S11.Sub", "Reiskaart.S11.Tip"),
            12 => new(12, 3, 8, false, true, "Reiskaart.S12.Kop", "Reiskaart.S12.Sub", "Reiskaart.S12.Tip"),
            13 => new(13, 3, 8, false, false, "Reiskaart.S13.Kop", "Reiskaart.S13.Sub", "Reiskaart.S13.Tip"),
            14 => new(14, 3, 9, false, false, "Reiskaart.S14.Kop", "Reiskaart.S14.Sub", "Reiskaart.S14.Tip"),
            15 => new(15, 4, 10, true, false, "Reiskaart.S15.Kop", "Reiskaart.S15.Sub", "Reiskaart.S15.Tip"),
            16 => new(16, 4, 10, true, false, "Reiskaart.S16.Kop", "Reiskaart.S16.Sub", "Reiskaart.S16.Tip"),
            _ => throw new ArgumentOutOfRangeException(nameof(screenNumber), screenNumber, "Reiskaart screen must be 1–16."),
        };

    internal static ScreenState ForOnboardingStep(int step) => ForScreen(ScreenForOnboardingStep(step));

    internal static readonly string[] ZoneNameKeys =
    [
        "Reiskaart.Zone.Strand",
        "Reiskaart.Zone.Kust",
        "Reiskaart.Zone.Rotsen",
        "Reiskaart.Zone.Diepte",
        "Reiskaart.Zone.Licht",
    ];
}
