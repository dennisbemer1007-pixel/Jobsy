namespace Jobsy.Core.Rules;

/// <summary>
/// Banenkaart "Toon mijn vacatures" — only the caller's own published, still-open vacancies.
/// </summary>
public static class ManagedVacancyMapFilter
{
    public static bool IsPublishedOpen(string? status, Guid? fulfilledByApplicationId)
        => string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
           && fulfilledByApplicationId is null;
}
