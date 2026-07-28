namespace Jobsy.Core.Entities;

public class MasterdataOption
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnCandidate { get; set; } = true;
    public bool ShowOnVacancy { get; set; } = true;
}
