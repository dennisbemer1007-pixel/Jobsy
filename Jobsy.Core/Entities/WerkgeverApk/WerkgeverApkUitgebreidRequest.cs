namespace Jobsy.Core.Entities.WerkgeverApk;

/// <summary>Manager asked admin to switch this company to Uitgebreid (not auto-applied).</summary>
public class WerkgeverApkUitgebreidRequest
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public bool Handled { get; set; }

    public DateTime? HandledAtUtc { get; set; }
}
