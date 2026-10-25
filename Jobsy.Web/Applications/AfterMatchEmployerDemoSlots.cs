using Jobsy.Core.Time;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerDemoSlots
{
    /// <summary>Demo slots for werkgever screen 03 (Europe/Amsterdam wall clock).</summary>
    internal static readonly DateTime[] InitialUtc =
    [
        AmsterdamTime.ToUtc(new DateTime(2026, 10, 22, 10, 30, 0, DateTimeKind.Unspecified)),
        AmsterdamTime.ToUtc(new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Unspecified)),
        AmsterdamTime.ToUtc(new DateTime(2026, 10, 26, 14, 0, 0, DateTimeKind.Unspecified)),
    ];
}
