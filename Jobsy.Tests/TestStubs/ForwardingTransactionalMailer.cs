using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;

namespace Jobsy.Tests.TestStubs;

/// <summary>
/// Adapts a legacy <see cref="IEmailService"/> capture/stub for services that now take
/// <see cref="ITransactionalMailer"/>.
/// </summary>
internal sealed class ForwardingTransactionalMailer(IEmailService inner) : ITransactionalMailer
{
    public async Task<EmailSendOutcome> SendAsync(
        ComposedEmail mail,
        string to,
        EmailSendOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        var delivery = await inner.SendAsync(
            new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category),
            cancellationToken);
        return new EmailSendOutcome(true, false, null, delivery.Kind);
    }
}
