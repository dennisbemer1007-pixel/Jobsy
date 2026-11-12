using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Tests.Scholen;

internal static class PupilG78RandomFixtures
{
    internal static (PupilResult Result, PupilProgress Progress) ForSeed(int seed)
        => (seed % 3) switch
        {
            0 => PupilG78RealisticFixtures.HelpsOthers(),
            1 => PupilG78RealisticFixtures.LeadsGroup(),
            _ => PupilG78RealisticFixtures.Maker(),
        };
}
