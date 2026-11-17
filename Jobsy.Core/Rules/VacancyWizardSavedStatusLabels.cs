using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Human-readable status on the vacancy wizard saved-confirmation screen.</summary>
public static class VacancyWizardSavedStatusLabels
{
    public static string ForStatusName(string? statusName) => statusName switch
    {
        nameof(VacancyStatus.Active) => "Actief, zichtbaar voor kandidaten",
        nameof(VacancyStatus.PendingApproval) => "Wacht op goedkeuring",
        _ => "Concept, nog niet zichtbaar",
    };

    public static bool IsPublishedStatus(string? statusName) => statusName is nameof(VacancyStatus.Active)
        or nameof(VacancyStatus.PendingApproval);
}