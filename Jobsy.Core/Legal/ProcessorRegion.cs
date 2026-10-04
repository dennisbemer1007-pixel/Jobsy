namespace Jobsy.Core.Legal;

/// <summary>Where a company is based, or where it processes personal data.</summary>
public enum ProcessorRegion
{
    Netherlands,
    EuropeanUnion,
    EuFrankfurt,
    France,
    Switzerland,
    UnitedKingdom,
    UnitedStates,
    EuAndUnitedStates,
    EuOrUnitedStates,
    OutsideEuropeanUnion
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
        ProcessorRegion.France => "Frankrijk (Parijs)",
        ProcessorRegion.Switzerland => "Zwitserland",
        ProcessorRegion.UnitedKingdom => "Verenigd Koninkrijk",
        ProcessorRegion.UnitedStates => "Verenigde Staten",
        ProcessorRegion.EuAndUnitedStates => "EU en Verenigde Staten",
        ProcessorRegion.EuOrUnitedStates => "EU of Verenigde Staten, afhankelijk van onze instelling",
        ProcessorRegion.OutsideEuropeanUnion => "buiten de EU",
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, null)
    };
}
