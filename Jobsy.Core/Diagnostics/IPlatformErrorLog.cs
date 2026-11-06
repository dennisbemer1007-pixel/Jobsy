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

    Task WriteAsync(
        string category,
        string message,
        string? supportCode,
        string? detail,
        Jobsy.Core.Enums.PlatformLogLevel level,
        CancellationToken cancellationToken = default)
        => WriteAsync(category, message, supportCode, detail, cancellationToken);
}
