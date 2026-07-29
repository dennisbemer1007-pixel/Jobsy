using Jobsy.Core.Rules;

namespace Jobsy.Web.Localization;

/// <summary>Maps stored Dutch codes / enums to the active UI language.</summary>
public static class UiLabels
{
    public static string WorkType(CultureState culture, string label) => label switch
    {
        WorkTypeLabels.Horeca => culture["WorkType.Horeca"],
        WorkTypeLabels.Winkel => culture["WorkType.Winkel"],
        WorkTypeLabels.Logistiek => culture["WorkType.Logistiek"],
        WorkTypeLabels.Tuinbouw => culture["WorkType.Tuinbouw"],
        WorkTypeLabels.Zorg => culture["WorkType.Zorg"],
        WorkTypeLabels.Kantoor => culture["WorkType.Kantoor"],
        WorkTypeLabels.Bouw => culture["WorkType.Bouw"],
        WorkTypeLabels.Schoonmaak => culture["WorkType.Schoonmaak"],
        WorkTypeLabels.Productie => culture["WorkType.Productie"],
        _ => label
    };

    public static string Transport(CultureState culture, string transport) => transport switch
    {
        "Fiets" => culture["Transport.Bike"],
        "Auto" => culture["Transport.Car"],
        "OV" => culture["Transport.Transit"],
        "Lopend" => culture["Transport.Walk"],
        _ => transport
    };

    public static string TransportVerb(CultureState culture, string transport) => transport switch
    {
        "Fiets" => culture["Transport.Verb.Bike"],
        "Auto" => culture["Transport.Verb.Car"],
        "OV" => culture["Transport.Verb.Transit"],
        "Lopend" => culture["Transport.Verb.Walk"],
        _ => culture["Transport.Verb.Default"]
    };

    public static string ApplicationStatus(CultureState culture, string status) => status switch
    {
        "Pending" => culture["Apps.Status.Pending"],
        "Accepted" => culture["Apps.Status.Accepted"],
        "Rejected" => culture["Apps.Status.Rejected"],
        "EmployerContacting" => culture["Apps.Status.EmployerContacting"],
        "Hired" => culture["Apps.Status.Hired"],
        "FilledElsewhere" => culture["Apps.Status.FilledElsewhere"],
        "Withdrawn" => culture["Apps.Status.Withdrawn"],
        _ => status
    };

    public static string Weekday(CultureState culture, string day) => day switch
    {
        "Ma" => culture["Profile.Day.Mon"],
        "Di" => culture["Profile.Day.Tue"],
        "Wo" => culture["Profile.Day.Wed"],
        "Do" => culture["Profile.Day.Thu"],
        "Vr" => culture["Profile.Day.Fri"],
        "Za" => culture["Profile.Day.Sat"],
        "Zo" => culture["Profile.Day.Sun"],
        _ => day
    };

    public static string AvailabilitySlot(CultureState culture, string slot) => slot switch
    {
        "Ochtend" => culture["Profile.Slot.Morning"],
        "Middag" => culture["Profile.Slot.Afternoon"],
        "Avond" => culture["Profile.Slot.Evening"],
        "Nacht" => culture["Profile.Slot.Night"],
        _ => slot
    };

    public static string MetricKey(CultureState culture, string key) => key.ToLowerInvariant() switch
    {
        "applications" => culture["Metrics.Applications"],
        "likes" => culture["Metrics.Likes"],
        "shares" => culture["Metrics.Shares"],
        "reactions" => culture["Metrics.Reactions"],
        _ => key
    };
}
