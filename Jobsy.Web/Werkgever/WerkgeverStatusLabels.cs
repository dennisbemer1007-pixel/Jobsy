namespace Jobsy.Web.Werkgever;

/// <summary>
/// Single map of vacancy + application status → localization keys (D11 / file 08).
/// </summary>
public static class WerkgeverStatusLabels
{
    public static string VacancyKey(string? status, bool pendingAsVmWaiting = false)
    {
        if (string.Equals(status, "PendingApproval", StringComparison.OrdinalIgnoreCase) && pendingAsVmWaiting)
        {
            return "WgVac.Status.PendingVm";
        }

        return status switch
        {
            "Active" => "WgVac.Status.Active",
            "PendingApproval" => "WgVac.Status.Pending",
            "Draft" => "WgVac.Status.Draft",
            "Archived" => "WgVac.Status.Archived",
            "Fulfilled" => "WgVac.Status.Fulfilled",
            "DraftIncomplete" => "WgVac.Status.Draft",
            _ => "WgVac.Status.Draft"
        };
    }

    public static string ApplicationKey(string? status) => status switch
    {
        "Pending" => "WgApp.Status.Pending",
        "Accepted" => "WgApp.Status.Accepted",
        "EmployerContacting" => "WgApp.Status.Invited",
        "Hired" => "WgApp.Status.Hired",
        "Rejected" => "WgApp.Status.Rejected",
        "FilledElsewhere" => "WgApp.Status.FilledElsewhere",
        "Withdrawn" => "WgApp.Status.Withdrawn",
        _ => "WgApp.Status.Pending"
    };

    public static string DashboardBranchKey(string? status) => status switch
    {
        "Achterstand" => "WgDash.Status.Achterstand",
        "Aandacht" => "WgDash.Status.Aandacht",
        _ => "WgDash.Status.OpSchema"
    };
}
