using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reminders;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Infrastructure.Reminders;

public sealed class WebPushReminderChannel(
    IPushNotificationService push,
    WebPushVapidKeyProvider vapid) : IReminderChannel
{
    public string Name => "push";

    public async Task<ReminderChannelResult> SendAsync(
        ReminderDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        if (!dispatch.SendPush || string.IsNullOrWhiteSpace(dispatch.Email))
        {
            return new ReminderChannelResult(false, "push-off");
        }

        if (!vapid.IsEnabled)
        {
            return new ReminderChannelResult(false, "push-not-configured");
        }

        var tests = string.Equals(dispatch.Kind, ComebackReminderKinds.BasicTests, StringComparison.Ordinal);
        var prefix = tests ? "Email.ComebackTests" : "Email.ComebackLookAgain";
        await push.SendAsync(
            new PushMessage(
                dispatch.Email,
                EmailStrings.Get(dispatch.Culture, prefix + ".PushTitle"),
                EmailStrings.Get(dispatch.Culture, prefix + ".PushBody"),
                dispatch.OpenUrl,
                EmailOptionalCategories.ComebackReminder),
            cancellationToken);
        return new ReminderChannelResult(true);
    }
}
