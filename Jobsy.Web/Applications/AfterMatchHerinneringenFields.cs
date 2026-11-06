using Jobsy.Core.Contracts;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal sealed record AfterMatchHerinneringenFields(
    string EmailSubject,
    string EmailTitle,
    string EmailLead,
    string WhenLine,
    string WhereLine,
    string TravelLine,
    string WeatherLine,
    string PushTwoHoursTitle,
    string PushTwoHoursBody,
    string PushDayBeforeTitle,
    string PushDayBeforeBody,
    string LockDateLabel);

internal static class AfterMatchHerinneringenFieldsBuilder
{
    internal static AfterMatchHerinneringenFields Build(
        CultureState culture,
        AfterMatchThreadDto thread,
        Guid applicationId)
    {
        if (applicationId == AfterMatchMailSsrDemoData.ScheduledApplicationId)
        {
            return DemoMockup(culture, thread);
        }

        var firstName = AfterMatchMailSsrFormatting.CandidateFirstName(thread);
        return new AfterMatchHerinneringenFields(
            string.Format(culture["AfterMatch.Screen07.ReminderEmailSubject"], thread.CompanyName),
            culture["AfterMatch.Screen07.ReminderEmailTitle"],
            string.Format(culture["AfterMatch.Screen07.ReminderEmailLead"], firstName),
            culture["AfterMatch.Screen07.ReminderWhenFallback"],
            thread.LocationOrLink ?? thread.CompanyName,
            thread.TravelLabel ?? "",
            "",
            culture["AfterMatch.Screen07.PushTwoHoursTitle"],
            string.Format(culture["AfterMatch.Screen07.PushTwoHoursBody"], thread.CompanyName),
            culture["AfterMatch.Screen07.PushDayBeforeTitle"],
            culture["AfterMatch.Screen07.PushDayBeforeBody"],
            culture["AfterMatch.Screen07.LockDateFallback"]);
    }

    private static AfterMatchHerinneringenFields DemoMockup(CultureState culture, AfterMatchThreadDto thread)
    {
        var firstName = AfterMatchMailSsrFormatting.CandidateFirstName(thread);
        return new AfterMatchHerinneringenFields(
            string.Format(culture["AfterMatch.Screen07.ReminderEmailSubject"], thread.CompanyName),
            culture["AfterMatch.Screen07.ReminderEmailTitle"],
            string.Format(culture["AfterMatch.Screen07.ReminderEmailLeadMock"], firstName),
            culture["AfterMatch.Screen07.ReminderWhenMock"],
            culture["AfterMatch.Screen07.ReminderWhereMock"],
            culture["AfterMatch.Screen07.ReminderTravelMock"],
            culture["AfterMatch.Screen07.ReminderWeatherMock"],
            culture["AfterMatch.Screen07.PushTwoHoursTitle"],
            culture["AfterMatch.Screen07.PushTwoHoursBodyMock"],
            culture["AfterMatch.Screen07.PushDayBeforeTitle"],
            culture["AfterMatch.Screen07.PushDayBeforeBodyMock"],
            culture["AfterMatch.Screen07.LockDateMock"]);
    }
}
