using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Interfaces;

public interface ITransactionalMailer
{
    Task<EmailSendOutcome> SendAsync(
        ComposedEmail mail,
        string to,
        EmailSendOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public sealed record EmailSendOptions(
    bool BypassSuppression = false,
    /// <summary>
    /// Optional Resend/SMTP idempotency key. Hosted jobs should pass
    /// <c>{key}:{entityId}:{yyyyMMdd}</c> so retries cannot double-send.
    /// </summary>
    string? IdempotencyKey = null,
    /// <summary>Recipient culture used for tags/lang; compose already baked the copy.</summary>
    EmailCulture? Culture = null,
    /// <summary>
    /// Admin catalog test send: no List-Unsubscribe, header <c>X-Lobsy-Test: 1</c>, tag <c>test=true</c>.
    /// </summary>
    bool IsTest = false,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public sealed record EmailSendOutcome(
    bool Sent,
    bool Suppressed,
    string? Reason,
    EmailDeliveryKind? DeliveryKind = null)
{
    public EmailDeliveryKind Kind => DeliveryKind ?? EmailDeliveryKind.Stub;

    public bool DeliveredViaProvider
        => Sent && !Suppressed && DeliveryKind == EmailDeliveryKind.Provider;
}
