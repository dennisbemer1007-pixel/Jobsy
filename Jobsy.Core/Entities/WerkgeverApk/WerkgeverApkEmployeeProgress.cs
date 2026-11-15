namespace Jobsy.Core.Entities.WerkgeverApk;

/// <summary>In-progress Loopbaan-APK answers for an employee code.</summary>
public class WerkgeverApkEmployeeProgress
{
    public Guid EmployeeCodeId { get; set; }
    public WerkgeverApkEmployeeCode? EmployeeCode { get; set; }

    /// <summary>JSON for <see cref="Scholen.MboHbo.PupilMboHboProgressState"/> (five-block flow).</summary>
    public string ProgressJson { get; set; } = "{}";

    public bool WelcomeSeen { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
