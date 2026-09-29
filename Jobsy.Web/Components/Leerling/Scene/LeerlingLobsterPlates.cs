using System.Globalization;

namespace Jobsy.Web.Components.Leerling.Scene;

/// <summary>Plate SVG paths from docs/mockups/scholen/base/ontdekkingsreis_build.py (10 plates).</summary>
public static class LeerlingLobsterPlates
{
    public const string BodyPath = "M95 150C93 190 112 214 128 241C144 214 165 190 165 150Z";

    public const string CracksPath =
        "M108 158l6 8-3 7M150 196l-5 6 3 5M120 222l5 6M104 128l5 4M160 142l-4 3M118 56l4 6";

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
        return string.Concat(Plates.Skip(gone).Select(p => string.Format(CultureInfo.InvariantCulture, p, maskId)));
    }
}

public static class LeerlingSceneCatalog
{
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
