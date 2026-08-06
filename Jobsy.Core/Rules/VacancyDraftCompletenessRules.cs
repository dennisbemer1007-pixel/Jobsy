using System.Text.Json;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

public static class VacancyDraftCompletenessRules
{
    public static bool IsIncomplete(Vacancy vacancy)
    {
        if (string.IsNullOrWhiteSpace(vacancy.Title)
            || string.IsNullOrWhiteSpace(vacancy.Description)
            || vacancy.SalaryTableId is null
            || !HasWorkType(vacancy)
            || vacancy.CategoryId is null
            || !vacancy.ContentModerationPassed)
        {
            return true;
        }

        return IsInclusive(vacancy)
            && !HasCategoryField(vacancy.CategoryFieldsJson, VacancyCategoryExtraFields.TargetGroup);
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
