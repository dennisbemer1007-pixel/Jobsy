namespace Jobsy.Core.Interfaces;

public interface IPushNotificationService
{
    Task SendAsync(PushMessage message, CancellationToken cancellationToken = default);
}

public record PushMessage(
    string UserEmail,
    string Title,
    string Body,
    string? DeepLink = null,
    string? Category = null);
