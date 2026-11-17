using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class VacancyWizardSavedStatusLabelsTests
{
    [Theory]
    [InlineData(nameof(VacancyStatus.Active), "Actief, zichtbaar voor kandidaten", true)]
    [InlineData(nameof(VacancyStatus.PendingApproval), "Wacht op goedkeuring", true)]
    [InlineData(nameof(VacancyStatus.Draft), "Concept, nog niet zichtbaar", false)]
    [InlineData(null, "Concept, nog niet zichtbaar", false)]
    public void Saved_status_label_matches_publish_state(string? status, string expected, bool published)
    {
        Assert.Equal(expected, VacancyWizardSavedStatusLabels.ForStatusName(status));
        Assert.Equal(published, VacancyWizardSavedStatusLabels.IsPublishedStatus(status));
    }
}