using Jobsy.Core.Enums;

namespace Jobsy.Core.Notifications;

/// <summary>Maps UserNotification.Category to preference soort for dispatcher (PR 6).</summary>
public static class StatusNotificationCategories
{
    public const string HerinneringPrefix = "InterviewReminder";

    public static NotificationPreferenceSoort SoortForCategory(string category)
    {
        if (category.StartsWith(HerinneringPrefix, StringComparison.Ordinal))
        {
            return NotificationPreferenceSoort.Herinnering;
        }

        if (category is "ApplicationNewMessage" or "ApplicationNewMessageSelf")
        {
            return NotificationPreferenceSoort.Bericht;
        }

        if (category.StartsWith("ApplicationOutcome", StringComparison.Ordinal))
        {
            return NotificationPreferenceSoort.Uitkomst;
        }

        if (category.StartsWith("Interview", StringComparison.Ordinal))
        {
            return NotificationPreferenceSoort.Gesprek;
        }

        return NotificationPreferenceSoort.Sollicitatie;
    }
}
