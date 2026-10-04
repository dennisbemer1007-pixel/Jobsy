namespace Jobsy.Core.Legal;

/// <summary>Where a company is based, or where it processes personal data.</summary>
public enum ProcessorRegion
{
    Netherlands,
    EuropeanUnion,
    EuFrankfurt,
    Switzerland,
    UnitedStates,
    EuAndUnitedStates,
    EuOrUnitedStates
}

/// <summary>Dutch labels for the official processor table. The privacy sentence uses its own translations.</summary>
public static class ProcessorRegionText
{
    public static string DutchWhere(ProcessorRegion companyHq, ProcessorRegion dataRegion)
        => $"Bedrijf: {Dutch(companyHq)}. Gegevens: {Dutch(dataRegion)}.";

    public static string Dutch(ProcessorRegion region) => region switch
    {
        ProcessorRegion.Netherlands => "Nederland",
        ProcessorRegion.EuropeanUnion => "EU",
        ProcessorRegion.EuFrankfurt => "EU (Frankfurt)",
        ProcessorRegion.Switzerland => "Zwitserland",
        ProcessorRegion.UnitedStates => "Verenigde Staten",
        ProcessorRegion.EuAndUnitedStates => "EU en Verenigde Staten",
        ProcessorRegion.EuOrUnitedStates => "EU of Verenigde Staten, afhankelijk van onze instelling",
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
    };
}
