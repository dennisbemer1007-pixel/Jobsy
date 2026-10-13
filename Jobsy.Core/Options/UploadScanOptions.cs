namespace Jobsy.Core.Options;

/// <summary>
/// Optional malware scan for candidate CV uploads. Disabled by default: only magic-byte checks run.
/// When <see cref="Enabled"/> is true and the scanner cannot be reached, uploads are rejected
/// outside Development unless <see cref="FailClosed"/> is explicitly false.
/// Acceptatie and production both run with <c>ASPNETCORE_ENVIRONMENT=Production</c>, so the default there is fail-closed.
/// </summary>
public sealed class UploadScanOptions
{
    public const string SectionName = "UploadScan";

    /// <summary>Off: do not contact a scanner. On: scan after the magic-byte gate.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <c>tcp://host:3310</c> (clamd INSTREAM) or an http(s) URL of a ClamAV REST wrapper.
    /// <c>clamav://host:3310</c> is the same TCP protocol.
    /// </summary>
    public string? Endpoint { get; set; }

    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Bytes above this size are not sent to the scanner. Default is the CV limit (5 MB).</summary>
    public int MaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Multipart field name for HTTP scans. Default <c>file</c>.
    /// The benzino77 ClamAV REST API expects <c>FILES</c>.
    /// </summary>
    public string HttpFieldName { get; set; } = "file";

    /// <summary>
    /// Null uses the environment default: fail-closed outside Development, fail-open in Development.
    /// An explicit true or false always wins, including on Acceptatie.
    /// </summary>
    public bool? FailClosed { get; set; }

    public bool RejectWhenUnavailable(bool isDevelopment)
        => FailClosed ?? !isDevelopment;

    public TimeSpan Timeout
        => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds <= 0 ? 15 : TimeoutSeconds, 1, 120));
}
