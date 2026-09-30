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

public sealed record EmailSendOptions(
    bool BypassSuppression = false);

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
