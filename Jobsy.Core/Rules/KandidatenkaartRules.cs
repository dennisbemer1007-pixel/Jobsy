namespace Jobsy.Core.Rules;

public static class KandidatenkaartRules
{
    public const int MinCandidatesPerArea = 30;
    public const int MinMatchPercentForInvite = 70;
    public const int MaxInvitesPerCandidatePerWeek = 3;
    public const int InviteReminderDays = 2;
    public const int AutoUnavailableAfterReminderDays = 2;
    public const int InactivityLoginDays = 30;
    public const int InactivityResponseDays = 7;
    public const int StagedRoundDelayHours = 4;

    public static readonly int[] AllowedMinMatchPercents = [60, 70, 80];
    public static readonly int[] AllowedTravelKm = [15, 25, 40, 60];
}
