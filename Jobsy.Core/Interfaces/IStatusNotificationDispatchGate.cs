namespace Jobsy.Core.Interfaces;

/// <summary>
/// Bouwspec §8 gate for statusmeldingen: voorkeur, stille uren (Europe/Amsterdam), dagmaximum.
/// Bel (UserNotification) blijft leidend; push/mail worden hier gefilterd.
/// </summary>
public interface IStatusNotificationDispatchGate
{
    Task<StatusNotificationDispatchDecision> EvaluateAsync(
        Guid userId,
        string category,
        CancellationToken cancellationToken = default);
}

public readonly record struct StatusNotificationDispatchDecision(
    bool AllowPush,
    bool AllowMail,
    bool AllowBell);
