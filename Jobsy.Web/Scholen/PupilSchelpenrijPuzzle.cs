namespace Jobsy.Web.Scholen;

/// <summary>
/// Schelpenrij groep 7/8: five shells + “?” slot on intro (mockups 03/06/09) and play.
/// Sizes 1–4 map to schelp-1 … schelp-4 images (2 = stekelhoorn, etc.).
/// </summary>
public static class PupilSchelpenrijPuzzle
{
    /// <summary>Correct left-to-right order (matches intro preview row).</summary>
    public static readonly int[] TargetOrder = [1, 2, 2, 3, 1];

    /// <summary>Starting order for play (one swap away from <see cref="TargetOrder"/>).</summary>
    public static readonly int[] InitialPlayOrder = [1, 3, 2, 2, 1];

    /// <summary>Shell size that completes the pattern after the “?” (mockup 07).</summary>
    public const int NextShellSize = 2;

    public static int ShellCount => TargetOrder.Length;

    public static IReadOnlyList<string> PreviewImageUrls { get; } =
        TargetOrder.Select(ShellImagePath).ToList();

    public static string ShellImagePath(int size)
        => size switch
        {
            1 => "/leerling/werelden/img/schelp-1.png",
            2 => "/leerling/werelden/img/schelp-2.png",
            3 => "/leerling/werelden/img/schelp-3.png",
            _ => "/leerling/werelden/img/schelp-4.png",
        };

    public static bool IsSolved(IReadOnlyList<int> order)
    {
        if (order.Count != TargetOrder.Length)
        {
            return false;
        }

        for (var i = 0; i < TargetOrder.Length; i++)
        {
            if (order[i] != TargetOrder[i])
            {
                return false;
            }
        }

        return true;
    }
}
