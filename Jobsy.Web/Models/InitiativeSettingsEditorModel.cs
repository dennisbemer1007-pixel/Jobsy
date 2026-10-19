namespace Jobsy.Web.Models;

public sealed class InitiativeSettingsEditorModel
{
    public bool IsEnabled { get; set; }
    public string ContactEmail { get; set; } = "";
    public bool ContactEmailConfirmed { get; set; }
    public int MaxPerWeek { get; set; } = 5;
    public List<InitiativeTeamEditorModel> Teams { get; set; } = [];
}

public sealed class InitiativeTeamEditorModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? BranchCompanyId { get; set; }
    public string ContactEmail { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public int MaxPerWeek { get; set; } = 5;
    public bool IsOpen { get; set; }
}
