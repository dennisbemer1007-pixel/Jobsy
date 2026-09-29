using System.Text.Json;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

public static class VacancyDraftCompletenessRules
{
    public static bool IsIncomplete(Vacancy vacancy) => CountMissingFields(vacancy) > 0;

    /// <summary>Number of required draft fields that are still empty.</summary>
    public static int CountMissingFields(Vacancy vacancy)
    {
        var missing = 0;
        if (string.IsNullOrWhiteSpace(vacancy.Title))
        {
            missing++;
        }

        if (string.IsNullOrWhiteSpace(vacancy.Description))
        {
            missing++;
        }

        if (vacancy.SalaryTableId is null)
        {
            missing++;
        }

        if (!HasWorkType(vacancy))
        {
            missing++;
        }

        if (vacancy.CategoryId is null)
        {
            missing++;
        }

        if (!vacancy.ContentModerationPassed)
        {
            missing++;
        }

        if (IsInclusive(vacancy)
            && !HasCategoryField(vacancy.CategoryFieldsJson, VacancyCategoryExtraFields.TargetGroup))
        {
            missing++;
        }

        return missing;
    }

    private static bool HasWorkType(Vacancy vacancy)
        => WorkTypeLabels.ResolveLabels(vacancy.WorkTypes, vacancy.WorkTypeLabels)?.Length > 0;

    private static bool IsInclusive(Vacancy vacancy)
        => vacancy.CategoryId == VacancyCategoryDefaults.InclusiefId
            || string.Equals(vacancy.Category?.Slug, "inclusief", StringComparison.OrdinalIgnoreCase);

    private static bool HasCategoryField(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return values is not null
                && values.TryGetValue(key, out var value)
                && !string.IsNullOrWhiteSpace(value);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
