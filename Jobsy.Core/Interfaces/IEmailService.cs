namespace Jobsy.Core.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    string? Category = null);
