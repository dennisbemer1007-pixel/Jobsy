using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkEmployeeScoring
{
    public static PupilMboHboScoring.BlockOutcome BuildInteresseOutcome(
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeAbCatalog catalog,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        _ = level;
        return WerkgeverApkAbScoring.BuildInteresseOutcome(progress, catalog);
    }
}