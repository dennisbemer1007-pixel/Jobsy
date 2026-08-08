namespace Jobsy.Core.Rules;

public static class CandidateActionPurposes
{
    public const string SetUnavailable = "SetUnavailable";
    public const string WithdrawOtherApplications = "WithdrawOtherApplications";

    public static bool IsKnown(string? purpose)
        => purpose is SetUnavailable or WithdrawOtherApplications;
}
