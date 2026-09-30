namespace Jobsy.Core.Rules;

public enum EngagementTipKind
{
    LowVisibility,
    ViewsNoApplications,
    ManyViewsFewApplications,
    NotShared,
    LowClickThrough,
    General
}

public static class VacancyEngagementReminderRules
{
    /// <summary>Days a vacancy must be Active before the engagement reminder is sent.</summary>
    public const int OpenDaysBeforeReminder = 14;

    /// <summary>Goodwill EndDate extension after the entrepreneur edits before the deadline.</summary>
    public const int GoodwillExtendDays = 7;

    public static bool IsEligibleForReminder(
        DateTime? publishedAtUtc,
        DateTime? reminderSentAtUtc,
        DateTime utcNow)
    {
        if (reminderSentAtUtc is not null || publishedAtUtc is null)
        {
            return false;
        }

        return publishedAtUtc.Value <= utcNow.AddDays(-OpenDaysBeforeReminder);
    }

    public static bool CanApplyGoodwillExtension(
        DateTime? reminderSentAtUtc,
        DateTime? goodwillExtendedAtUtc,
        DateOnly endDate,
        DateOnly today)
        => reminderSentAtUtc is not null
           && goodwillExtendedAtUtc is null
           && today <= endDate;

    public static EngagementTipKind BuildHeuristicTipKind(
        int searchAppearances,
        int views,
        int shares,
        int saved,
        int applications)
    {
        if (applications == 0 && views < 5)
        {
            return EngagementTipKind.LowVisibility;
        }

        if (applications == 0 && views >= 5)
        {
            return EngagementTipKind.ViewsNoApplications;
        }

        if (applications > 0 && applications < 3 && views > 20)
        {
            return EngagementTipKind.ManyViewsFewApplications;
        }

        if (shares == 0 && saved == 0)
        {
            return EngagementTipKind.NotShared;
        }

        if (searchAppearances > 50 && views < 10)
        {
            return EngagementTipKind.LowClickThrough;
        }

        return EngagementTipKind.General;
    }

    /// <summary>Dutch tip text (nl catalog). Prefer <see cref="BuildHeuristicTipKind"/> + EmailStrings.</summary>
    public static string BuildHeuristicTip(
        int searchAppearances,
        int views,
        int shares,
        int saved,
        int applications)
    {
        var kind = BuildHeuristicTipKind(searchAppearances, views, shares, saved, applications);
        return Email.Localization.EmailStrings.Get("nl", $"Email.VacancyEngagementReminder.Tip.{kind}");
    }
}
