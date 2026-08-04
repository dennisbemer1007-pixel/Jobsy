namespace Jobsy.Core.Entities;

/// <summary>
/// Masterdata: exclusiviteitsinstelling voor stageplekken (open of school-specifiek).
/// </summary>
public class ExclusivitySetting
{
    public Guid Id { get; set; }

    /// <summary>Weergavenaam, bijv. "Exclusief voor Inholland" of "Open voor alle studenten".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>School e-maildomein zonder @, bijv. "student.inholland.nl". Null voor open-optie.</summary>
    public string? SchoolDomain { get; set; }

    /// <summary>Optionele regex voor studentnummer-validatie.</summary>
    public string? StudentNumberPattern { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>True for the single "Open voor alle studenten" option.</summary>
    public bool IsOpenOption { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ExclusivityEducation> Educations { get; set; } = new List<ExclusivityEducation>();
    public ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();
}
