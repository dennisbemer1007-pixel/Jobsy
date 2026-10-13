using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Reminders;

/// <summary>
/// One way to deliver a come-back reminder. Channels never send unless the dispatch
/// says this person opted in for that channel.
/// </summary>
public interface IReminderChannel
{
    string Name { get; }

    Task<ReminderChannelResult> SendAsync(ReminderDispatch dispatch, CancellationToken cancellationToken = default);
}

public sealed record ReminderDispatch(
    Guid UserId,
    string Email,
    string? FullName,
    string Kind,
    EmailCulture Culture,
    string PublicWebBaseUrl,
    DateTime SentAtUtc,
    bool SendEmail,
    bool SendPush,
    bool SendWhatsApp,
    string? WhatsAppPhone)
{
    public string OpenUrl
    {
        get
        {
            var path = string.Equals(Kind, ComebackReminderKinds.BasicTests, StringComparison.Ordinal)
                ? "/profiel/tests"
                : "/profiel";
            return EmailLinks.For(PublicWebBaseUrl).Absolute(path);
        }
    }
}

public sealed record ReminderChannelResult(bool Sent, string? SkipReason = null);
