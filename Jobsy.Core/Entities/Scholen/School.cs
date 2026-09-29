namespace Jobsy.Core.Entities.Scholen;

/// <summary>
/// School organisation. Lobsy is processor; the school is controller (verwerkersovereenkomst).
/// No pupil PII is stored under a school.
/// </summary>
public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    /// <summary>Public Dutch school code (BRIN), optional.</summary>
    public string? BrinCode { get; set; }
    /// <summary>JSON array of allowed e-mail domains, e.g. <c>["voorbeeldcollege.nl"]</c>.</summary>
    public string AllowedEmailDomains { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateOnly? ProcessorAgreementSignedOn { get; set; }
    public string? ProcessorAgreementVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
}
