using Jobsy.Core.Entities;

namespace Jobsy.Core.Entities.WerkgeverApk;

/// <summary>Employer team for Loopbaan-APK (e.g. shift or department).</summary>
public class WerkgeverApkTeam
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<WerkgeverApkEmployeeCode> Codes { get; set; } = [];
}
