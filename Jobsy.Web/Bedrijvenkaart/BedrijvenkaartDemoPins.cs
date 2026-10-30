using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;

namespace Jobsy.Web.Bedrijvenkaart;

/// <summary>Demo pins aligned with Dennis mockups (Den Haag area).</summary>
public static class BedrijvenkaartDemoPins
{
    public static IReadOnlyList<CompanyMapPinDto> All { get; } =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111101"), Guid.Parse("11111111-1111-1111-1111-111111111101"), "Bakkerij De Korenaar", "Bakkerij", "Den Haag Centrum", 52.078, 4.307, true, false, 3, 2, InitiativeMatchLabel.Good, 2, "12345678", "/12345678"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111102"), Guid.Parse("11111111-1111-1111-1111-111111111102"), "TechnoDynamics", "Techniek", "Den Haag", 52.065, 4.325, true, false, 5, 1, InitiativeMatchLabel.Strong, 3, "23456789", "/23456789"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111103"), Guid.Parse("11111111-1111-1111-1111-111111111103"), "ZorgHuis Den Haag", "Zorg", "Den Haag", 52.072, 4.295, true, false, 0, 3, InitiativeMatchLabel.Good, 2, "34567890", "/34567890"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111104"), Guid.Parse("11111111-1111-1111-1111-111111111104"), "Randstad", "Uitzendbureau", "Den Haag", 52.081, 4.315, true, true, 12, 0, InitiativeMatchLabel.Reasonable, 4, "45678901", "/45678901"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111105"), Guid.Parse("11111111-1111-1111-1111-111111111105"), "Gemeente Den Haag", "Overheid", "Den Haag", 52.077, 4.300, false, false, 8, 0, InitiativeMatchLabel.Reasonable, null, "56789012", "/56789012"),
    ];
}
