using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reminders;

namespace Jobsy.Infrastructure.Reminders;

public sealed class EmailReminderChannel(ITransactionalMailer mailer) : IReminderChannel
{
    public string Name => "email";

    public async Task<ReminderChannelResult> SendAsync(
        ReminderDispatch dispatch,
        CancellationToken cancellationToken = default)
    {
        if (!dispatch.SendEmail || string.IsNullOrWhiteSpace(dispatch.Email))
        {
            return new ReminderChannelResult(false, "email-off");
        }

        var mail = TransactionalEmails.ComebackReminder(
            dispatch.PublicWebBaseUrl,
            dispatch.FullName,
            dispatch.Kind,
            dispatch.Culture);
        var day = dispatch.SentAtUtc.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var outcome = await mailer.SendAsync(
            mail,
            dispatch.Email,
            new EmailSendOptions(
                IdempotencyKey: $"comeback:{dispatch.UserId:N}:{dispatch.Kind}:{day}",
                Culture: dispatch.Culture),
            cancellationToken);
        return outcome.Sent && !outcome.Suppressed
            ? new ReminderChannelResult(true)
            : new ReminderChannelResult(false, outcome.Reason ?? "email-not-sent");
    }
}
