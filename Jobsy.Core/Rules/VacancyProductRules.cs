namespace Jobsy.Core.Rules;

public static class VacancyProductRules
{
    /// <summary>Default length added when extending a vacancy.</summary>
    public const int ExtendDays = 14;

    /// <summary>Fallback PushBom radius when no admin settings row exists.</summary>
    public const double PushBomRadiusKm = 10;

    /// <summary>Fallback max travel minutes when no admin settings row exists.</summary>
    public const int PushBomMaxTravelMinutes = 30;
}
