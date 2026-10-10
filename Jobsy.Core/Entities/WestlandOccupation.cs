namespace Jobsy.Core.Entities;

public class WestlandOccupation
{
    public Guid Id { get; set; }
    public string EscoId { get; set; } = "";
    public string TitleNl { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }

    public List<WestlandOccupationTask> Tasks { get; set; } = [];
}
