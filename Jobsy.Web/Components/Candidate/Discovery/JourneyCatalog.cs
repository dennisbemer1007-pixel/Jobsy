namespace Jobsy.Web.Components.Candidate.Discovery;

/// <summary>Plate SVG paths and size helpers from docs/mockups/ontdekkingsreis/build.py.</summary>
public static class JourneyLobsterPlates
{
    public const string BodyPath = "M95 150C93 190 112 214 128 241C144 214 165 190 165 150Z";

    public const string CracksPath =
        "M108 158l6 8-3 7M150 196l-5 6 3 5M120 222l5 6M104 128l5 4M160 142l-4 3M118 56l4 6";

    /// <summary>Plates in shed order (step 1..10): claws, tail tip, 3 tail segments, 2 chest, 2 helmet halves.</summary>
    public static readonly string[] Plates =
    [
        """<path d="M40 70C36 46 52 32 66 36L70 54 60 60 64 84C54 90 42 84 40 70Z"/>""",
        """<path d="M182 132C198 124 216 136 216 158 216 176 204 190 190 190L186 172 196 160 180 150Z"/>""",
        """<rect x="80" y="210" width="96" height="34" mask="url(#{0})"/>""",
        """<rect x="80" y="192" width="96" height="16.5" mask="url(#{0})"/>""",
        """<rect x="80" y="171" width="96" height="19.5" mask="url(#{0})"/>""",
        """<rect x="80" y="150" width="96" height="19.5" mask="url(#{0})"/>""",
        """<rect x="93" y="136" width="74" height="13" rx="6.5"/>""",
        """<rect x="97" y="121" width="66" height="13" rx="6.5"/>""",
        """<path d="M99 74C98 58 112 50 134 50L134 64C120 64 108 68 99 74Z"/>""",
        """<path d="M136 50C158 50 172 58 171 74 162 68 150 64 136 64Z"/>"""
    ];

    public static int SizeForStep(int step, bool mobile)
        => mobile ? 60 + Math.Clamp(step, 0, 11) * 4 : 120 + Math.Clamp(step, 0, 11) * 10;

    public static string PlateMarkup(int gone, string maskId)
    {
        gone = Math.Clamp(gone, 0, Plates.Length);
        return string.Concat(Plates.Skip(gone).Select(p => string.Format(p, maskId)));
    }
}

/// <summary>Step metadata mirrored from build.py STEPS / ZONES / SAY / GRAD.</summary>
public static class JourneyCatalog
{
    public sealed record StepInfo(int Number, string TitleKey, string SubKey, string ZoneKey);

    public static readonly StepInfo[] Steps =
    [
        new(1, "Discovery.Step1.Title", "Discovery.Step1.Sub", "kust"),
        new(2, "Discovery.Step2.Title", "Discovery.Step2.Sub", "kust"),
        new(3, "Discovery.Step3.Title", "Discovery.Step3.Sub", "rots"),
        new(4, "Discovery.Step4.Title", "Discovery.Step4.Sub", "rots"),
        new(5, "Discovery.Step5.Title", "Discovery.Step5.Sub", "rots"),
        new(6, "Discovery.Step6.Title", "Discovery.Step6.Sub", "rots"),
        new(7, "Discovery.Step7.Title", "Discovery.Step7.Sub", "diep"),
        new(8, "Discovery.Step8.Title", "Discovery.Step8.Sub", "diep"),
        new(9, "Discovery.Step9.Title", "Discovery.Step9.Sub", "diep"),
        new(10, "Discovery.Step10.Title", "Discovery.Step10.Sub", "diep")
    ];

    public static readonly IReadOnlyDictionary<string, string> ZoneKeys = new Dictionary<string, string>
    {
        ["kust"] = "Discovery.Zone.Coast",
        ["rots"] = "Discovery.Zone.Rocks",
        ["diep"] = "Discovery.Zone.Deep"
    };

    /// <summary>Depth 0 = start, 1–10 = steps, 11 = end.</summary>
    public static int DepthForScreen(string? stap)
    {
        if (string.IsNullOrWhiteSpace(stap) || stap.Equals("start", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (stap.Equals("klaar", StringComparison.OrdinalIgnoreCase)
            || stap.Equals("toestemming", StringComparison.OrdinalIgnoreCase))
        {
            return stap.Equals("klaar", StringComparison.OrdinalIgnoreCase) ? 11 : 6;
        }

        return int.TryParse(stap, out var n) ? Math.Clamp(n, 0, 11) : 0;
    }

    public static string ZoneEyebrowKey(int depth) => depth switch
    {
        0 => "Discovery.Zone.Beach",
        <= 2 => "Discovery.Zone.Coast",
        <= 6 => "Discovery.Zone.Rocks",
        <= 10 => "Discovery.Zone.Deep",
        _ => "Discovery.Zone.Light"
    };

    public static string SayKey(int depth) => $"Discovery.Say.{Math.Clamp(depth, 0, 11)}";

    public static readonly IReadOnlyDictionary<int, string> Gradients = new Dictionary<int, string>
    {
        [0] = "var(--sky) 0%,var(--sea-0) 44%,var(--sea-1) 60%,var(--sand) 100%",
        [1] = "var(--sky) 0%,var(--sky) 16%,var(--sea-1) 16.3%,var(--sea-3) 100%",
        [2] = "var(--sky) 0%,var(--sky) 5%,var(--sea-1) 5.3%,var(--sea-3) 80%,var(--sea-4) 100%",
        [3] = "var(--sea-1) 0%,var(--sea-3) 100%",
        [4] = "var(--sea-2) 0%,var(--sea-4) 100%",
        [5] = "var(--sea-2) 0%,var(--sea-5) 100%",
        [6] = "var(--sea-3) 0%,var(--sea-5) 100%",
        [7] = "var(--sea-6) 0%,var(--sea-7) 100%",
        [8] = "var(--sea-7) 0%,var(--sea-8) 100%",
        [9] = "var(--sea-7) 0%,var(--sea-9) 100%",
        [10] = "var(--sea-8) 0%,var(--sea-9) 100%",
        [11] = "var(--sky) 0%,var(--sea-0) 30%,var(--sea-2) 70%,var(--sea-4) 100%"
    };
}
