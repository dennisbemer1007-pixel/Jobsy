using Jobsy.Core.Enums;
using Jobsy.Core.Notifications;

namespace Jobsy.Tests;

public sealed class StatusNotificationCategoriesTests
{
    [Theory]
    [InlineData("EmployerNewApplication", NotificationPreferenceSoort.Sollicitatie)]
    [InlineData("InterviewScheduled", NotificationPreferenceSoort.Gesprek)]
    [InlineData("ApplicationNewMessage", NotificationPreferenceSoort.Bericht)]
    [InlineData("ApplicationOutcomeOffer", NotificationPreferenceSoort.Uitkomst)]
    [InlineData("InterviewReminderDayBefore", NotificationPreferenceSoort.Herinnering)]
    public void SoortForCategory_maps_doc_groups(string category, NotificationPreferenceSoort expected)
        => Assert.Equal(expected, StatusNotificationCategories.SoortForCategory(category));
}
