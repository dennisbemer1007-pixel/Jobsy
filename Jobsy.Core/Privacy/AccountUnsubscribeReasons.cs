namespace Jobsy.Core.Privacy;

/// <summary>Fixed reason codes for candidate account unsubscribe (uitschrijven).</summary>
public static class AccountUnsubscribeReasons
{
    public const string FoundJob = "found_job";
    public const string NotRelevant = "not_relevant";
    public const string TooManyEmails = "too_many_emails";
    public const string Privacy = "privacy";
    public const string OtherPlatform = "other_platform";
    public const string Temporary = "temporary";
    public const string Other = "other";

    public static IReadOnlyList<(string Code, string Label)> All { get; } =
    [
        (FoundJob, "Ik heb werk gevonden"),
        (NotRelevant, "Ik vind geen relevante vacatures"),
        (TooManyEmails, "Te veel berichten of meldingen"),
        (Privacy, "Privacyoverwegingen"),
        (OtherPlatform, "Ik gebruik een ander platform"),
        (Temporary, "Tijdelijk pauze / even niet zoeken"),
        (Other, "Anders")
    ];

    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && All.Any(r => string.Equals(r.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string GetLabel(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Onbekend";
        }

        var match = All.FirstOrDefault(r =>
            string.Equals(r.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(match.Code) ? code.Trim() : match.Label;
    }

    public static bool RequiresOtherText(string? code) =>
        string.Equals(code?.Trim(), Other, StringComparison.OrdinalIgnoreCase);
}
