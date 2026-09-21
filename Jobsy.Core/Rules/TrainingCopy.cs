namespace Jobsy.Core.Rules;

/// <summary>Candidate-facing copy for subtiele, in-context opleidingssuggesties (geen schreeuwende marketing).</summary>
public static class TrainingCopy
{
    public const string GapAdvice =
        "Een korte cursus kan dit stukje aanvullen.";

    public const string Cta =
        "Open deze cursus";

    public const string RegionalHint =
        "Eerst lokale praktijkopleiders in Den Haag en het Westland; landelijke cursussen als vangnet.";

    public const string SkillAdvice =
        "Wil je deze vaardigheid versterken? Er is een passende workshop.";

    public const string SkillCta =
        "Bekijk de workshop";

    public const string InContextTitle =
        "Passende cursus";

    /// <summary>Legacy CTA wording kept for migrations that assert the flywheel copy.</summary>
    public const string LegacyGapAdvice =
        "Volg een korte cursus of omscholing om dit gat te dichten.";
}
