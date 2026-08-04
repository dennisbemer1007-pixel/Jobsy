namespace Jobsy.Core.Entities;

/// <summary>Opleiding gekoppeld aan een exclusiviteitsinstelling.</summary>
public class ExclusivityEducation
{
    public Guid Id { get; set; }
    public Guid ExclusivitySettingId { get; set; }
    public ExclusivitySetting ExclusivitySetting { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
