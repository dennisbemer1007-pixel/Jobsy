using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Werkgever.Sections;

public partial class CompanyDetailsSection
{
    /// <summary>profile | contact | recruitment | api | csv | invoices-link | all</summary>
    [Parameter]
    public string Mode { get; set; } = "all";

    private bool IsEmbedded => !string.Equals(Mode, "all", StringComparison.OrdinalIgnoreCase);

    private bool Show(params string[] modes) =>
        string.Equals(Mode, "all", StringComparison.OrdinalIgnoreCase)
        || modes.Any(m => string.Equals(Mode, m, StringComparison.OrdinalIgnoreCase));
}
