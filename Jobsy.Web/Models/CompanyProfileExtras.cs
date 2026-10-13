namespace Jobsy.Web.Models;

public sealed class CompanyProfileExtras
{
    public Guid CompanyId { get; set; }
    public Guid RootCompanyId { get; set; }
    public List<string> WorkTypeLabels { get; set; } = [];
    public List<string> SuggestedWorkTypeLabels { get; set; } = [];
    public Dictionary<string, int>? CultureSliders { get; set; }
    public string? CultureSource { get; set; }
    public CulturePersonalityScoreSet? CultureScores { get; set; }
    public List<string> ValueCardIds { get; set; } = [];
    public SchwartzValuesScoreSet? ValuesScores { get; set; }
    public bool WorkTypesFromKvk { get; set; }
}
