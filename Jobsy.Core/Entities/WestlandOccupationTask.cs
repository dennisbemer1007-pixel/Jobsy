namespace Jobsy.Core.Entities;

public class WestlandOccupationTask
{
    public Guid Id { get; set; }
    public Guid OccupationId { get; set; }
    public WestlandOccupation Occupation { get; set; } = null!;

    public string TitleNl { get; set; } = "";
    public int SortOrder { get; set; }

    /// <summary>Only gecontroleerde taken tellen mee in pilot-rapportage.</summary>
    public bool Gecontroleerd { get; set; }

    public List<CandidateWestlandTaskChoice> Choices { get; set; } = [];
}
