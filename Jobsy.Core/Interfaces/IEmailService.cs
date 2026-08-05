namespace Jobsy.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends mail via the configured provider (Resend/SMTP) or the in-process stub.
    /// Returns whether a real provider accepted the message (not the PlatformLog stub).
    /// </summary>
    Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    string? Category = null);

public enum EmailDeliveryKind
{
    /// <summary>Logged to PlatformLogs only — no external SMTP/Resend delivery.</summary>
    Stub = 0,
    /// <summary>Accepted by Resend or SMTP.</summary>
    Provider = 1
}

public record EmailDeliveryResult(EmailDeliveryKind Kind)
{
    public bool DeliveredViaProvider => Kind == EmailDeliveryKind.Provider;

    public static EmailDeliveryResult Stub { get; } = new(EmailDeliveryKind.Stub);
    public static EmailDeliveryResult Provider { get; } = new(EmailDeliveryKind.Provider);
}
