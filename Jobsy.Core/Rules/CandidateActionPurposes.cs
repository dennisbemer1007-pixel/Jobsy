namespace Jobsy.Core.Rules;

public static class CandidateActionPurposes
{
    public const string SetUnavailable = "SetUnavailable";
    public const string WithdrawOtherApplications = "WithdrawOtherApplications";

    public static bool IsKnown(string? purpose)
        => purpose is SetUnavailable or WithdrawOtherApplications;

    /// <summary>In-app CTA path (no bearer token — requires authenticated session).</summary>
    public const string SetUnavailableInAppPath = "/candidate/actions/set-unavailable";

    /// <summary>In-app CTA path for withdrawing other applications after a hire.</summary>
    public static string WithdrawOthersInAppPath(Guid hiredApplicationId)
        => $"/candidate/actions/withdraw-others?hiredApplicationId={hiredApplicationId:D}";
}
