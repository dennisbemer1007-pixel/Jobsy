using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Localized duration fragments for passport course cards (keys resolved by UI).</summary>
public static class TrainingDurationFormat
{
    public static string? UnitKey(TrainingDurationUnit? unit) => unit switch
    {
        TrainingDurationUnit.Hours => "Passport.Course.Duration.Hours",
        TrainingDurationUnit.Days => "Passport.Course.Duration.Days",
        TrainingDurationUnit.Weeks => "Passport.Course.Duration.Weeks",
        TrainingDurationUnit.Months => "Passport.Course.Duration.Months",
        TrainingDurationUnit.Years => "Passport.Course.Duration.Years",
        _ => null
    };

    public static string TypeKey(TrainingOfferType type) => type switch
    {
        TrainingOfferType.Opleiding => "Passport.Course.Type.Opleiding",
        TrainingOfferType.Workshop => "Passport.Course.Type.Workshop",
        _ => "Passport.Course.Type.Cursus"
    };

    public static string DeliveryKey(TrainingDeliveryMode mode) => mode switch
    {
        TrainingDeliveryMode.OnSite => "Passport.Course.Delivery.OnSite",
        TrainingDeliveryMode.Blended => "Passport.Course.Delivery.Blended",
        _ => "Passport.Course.Delivery.Online"
    };
}
