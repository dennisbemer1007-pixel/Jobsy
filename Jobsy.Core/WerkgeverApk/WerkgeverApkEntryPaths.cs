using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkEntryPaths
{
    public const string Login = "/loopbaan-apk";
    public const string Welkom = "/loopbaan-apk/welkom";
    public const string Delen = "/loopbaan-apk/delen";
    public const string Vergelijking = "/loopbaan-apk/vergelijking";

    public static readonly PupilMboHboBlock[] EmployeeBlocks =
    [
        PupilMboHboBlock.Interesse,
        PupilMboHboBlock.Competentie,
        PupilMboHboBlock.Werkwaarden
    ];

    public static string Test(string blockKey) => $"/loopbaan-apk/test/{blockKey}";

    public static string Test(PupilMboHboBlock block)
        => Test(PupilMboHboProgressState.BlockKey(block));

    public static bool AllEmployeeBlocksComplete(PupilMboHboProgressState state)
        => EmployeeBlocks.All(state.IsBlockComplete);

    public static string AfterLogin(bool welcomeSeen, PupilMboHboProgressState state, bool completed, bool sharedWithManager = false)
    {
        if (completed)
        {
            return sharedWithManager ? Vergelijking : Delen;
        }

        if (!welcomeSeen)
        {
            return Welkom;
        }

        foreach (var block in EmployeeBlocks)
        {
            if (!state.IsBlockComplete(block))
            {
                return Test(block);
            }
        }

        return Delen;
    }
}
