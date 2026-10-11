namespace Jobsy.Core.Ops;

/// <summary>
/// Render / environment configuration for the break-glass system administrator.
/// Bind via <c>SystemAdmin__*</c> env vars or <c>SystemAdmin</c> section.
/// </summary>
public sealed class SystemAdminOptions
{
    public const string SectionName = "SystemAdmin";

    public bool Enabled { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool RequireTwoFactor { get; set; }

    /// <summary>Optional extra inbox for login alert e-mails (defaults to <see cref="Email"/> only).</summary>
    public string? NotifyEmail { get; set; }
}
