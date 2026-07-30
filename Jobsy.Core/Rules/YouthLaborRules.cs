namespace Jobsy.Core.Rules;

/// <summary>
/// Arbeidstijdenwet-style task flags on vacancies. No UI minimum age —
/// eligibility is derived from these flags + candidate age.
/// </summary>
public sealed class LegalTaskFlags
{
    public bool WorksAfter19 { get; set; }
    public bool NightShift23To06 { get; set; }
    public bool AdultSupervisorPresent { get; set; } = true;
    public bool HandlesMoneyOrClosing { get; set; }
    public bool HeavyOrHazardousWork { get; set; }

    public static IReadOnlyList<LegalTaskInfo> Catalog { get; } =
    [
        new(
            nameof(WorksAfter19),
            "Wordt er gewerkt na 19:00 uur?",
            "Indien aangevinkt, sluit het systeem sollicitanten van 15 jaar automatisch uit. Personen van 15 jaar mogen wettelijk niet na 19:00 uur werken."),
        new(
            nameof(NightShift23To06),
            "Wordt er gewerkt tussen 23:00 en 06:00 uur (Nachtdienst)?",
            "Indien aangevinkt, sluit het systeem alle personen van 15, 16 en 17 jaar automatisch uit. Nachtdienst is wettelijk verboden voor iedereen onder de 18 jaar."),
        new(
            nameof(AdultSupervisorPresent),
            "Is er te allen tijde een volwassen toezichthouder/begeleider aanwezig?",
            "Indien uitgeschakeld, sluit het systeem sollicitanten van 15 jaar automatisch uit. Zij mogen wettelijk nooit solowerk verrichten."),
        new(
            nameof(HandlesMoneyOrClosing),
            "Wordt er gewerkt met geld, kassasystemen of zelfstandig sluiten?",
            "Indien van toepassing, kan dit op basis van wettelijk toezicht specifiek de jongste categorie van 15 jaar uitsluiten voor deze handelingen."),
        new(
            nameof(HeavyOrHazardousWork),
            "Omvat de taak zwaar tilwerk of gevaarlijke handelingen/machines?",
            "Indien aangevinkt, filtert het systeem automatisch alle personen van 15, 16 en 17 jaar die volgens de strenge arboregels dit specifieke zware of gevaarlijke werk niet mogen verrichten.")
    ];
}

public readonly record struct LegalTaskInfo(string Code, string Label, string Tooltip);

public static class YouthLaborRules
{
    public const int MinWorkingAgeYears = 15;
    public const int AdultAgeYears = 18;

    public static YouthLaborEligibility Evaluate(int? ageYears, LegalTaskFlags? flags)
    {
        if (ageYears is null)
        {
            return YouthLaborEligibility.UnknownAge();
        }

        var age = ageYears.Value;
        if (age < MinWorkingAgeYears)
        {
            return YouthLaborEligibility.Blocked(["BelowMinWorkingAge"]);
        }

        if (flags is null || age >= AdultAgeYears)
        {
            return YouthLaborEligibility.Eligible();
        }

        var reasons = new List<string>();
        if (age == 15)
        {
            if (flags.WorksAfter19) reasons.Add("WorksAfter19");
            if (flags.NightShift23To06) reasons.Add("NightShift23To06");
            if (!flags.AdultSupervisorPresent) reasons.Add("NoAdultSupervisor");
            if (flags.HandlesMoneyOrClosing) reasons.Add("HandlesMoneyOrClosing");
            if (flags.HeavyOrHazardousWork) reasons.Add("HeavyOrHazardousWork");
        }
        else if (age is 16 or 17)
        {
            if (flags.NightShift23To06) reasons.Add("NightShift23To06");
            if (flags.HeavyOrHazardousWork) reasons.Add("HeavyOrHazardousWork");
        }

        return reasons.Count == 0
            ? YouthLaborEligibility.Eligible()
            : YouthLaborEligibility.Blocked(reasons);
    }

    public static string FriendlyBlockMessage
        => "Op basis van de wettelijke regels voor deze werkzaamheden kun je op deze leeftijd niet op deze vacature solliciteren.";
}

public readonly record struct YouthLaborEligibility(bool IsEligible, bool AgeKnown, IReadOnlyList<string> BlockReasons)
{
    public static YouthLaborEligibility Eligible()
        => new(true, true, Array.Empty<string>());

    public static YouthLaborEligibility UnknownAge()
        => new(true, false, Array.Empty<string>());

    public static YouthLaborEligibility Blocked(IReadOnlyList<string> reasons)
        => new(false, true, reasons);
}
