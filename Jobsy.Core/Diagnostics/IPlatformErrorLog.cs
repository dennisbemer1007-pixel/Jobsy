namespace Jobsy.Core.Diagnostics;

/// <summary>Writes a row to Systeemlogs (PlatformLogs) for a server-side failure.</summary>
public interface IPlatformErrorLog
{
    Task WriteAsync(
        string category,
        string message,
        string? supportCode,
        string? detail,
        CancellationToken cancellationToken = default);
}
