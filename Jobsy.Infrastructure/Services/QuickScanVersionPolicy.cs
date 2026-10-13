using Jobsy.Core.Enums;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Picks Quick-Scan catalog version for new candidate rows; existing rows keep their stored version.
/// </summary>
public static class QuickScanVersionPolicy
{
    public static int ResolveVersionForNew(int? existingRowVersion, bool testsV2Enabled)
    {
        if (existingRowVersion is int stored)
        {
            return AssessmentTestVersions.Normalize(stored);
        }

        return testsV2Enabled ? AssessmentTestVersions.V3 : AssessmentTestVersions.V1;
    }
}
