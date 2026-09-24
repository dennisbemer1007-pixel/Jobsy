namespace Jobsy.Core.Rules;

/// <summary>Parser quality score (0–100) for admin review of scraped listings.</summary>
public static class AtsCompletenessScore
{
    public static int Compute(
        string? title,
        string? companyName,
        string? location,
        string? description,
        string? salaryText,
        decimal? hourlyWage,
        string? hoursText,
        decimal? minHours,
        decimal? maxHours,
        string? tagsJson,
        string? sourceUrl,
        string? startDateText = null,
        string? requirementsText = null)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(title)) score += 15;
        if (!string.IsNullOrWhiteSpace(companyName)) score += 10;
        if (!string.IsNullOrWhiteSpace(location)) score += 10;
        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length >= 80) score += 20;
        else if (!string.IsNullOrWhiteSpace(description)) score += 10;
        if (!string.IsNullOrWhiteSpace(salaryText) || hourlyWage is > 0) score += 10;
        if (!string.IsNullOrWhiteSpace(hoursText) || minHours is > 0 || maxHours is > 0) score += 10;
        if (!string.IsNullOrWhiteSpace(startDateText)) score += 5;
        if (!string.IsNullOrWhiteSpace(requirementsText) && requirementsText.Trim().Length >= 20) score += 10;
        else if (!string.IsNullOrWhiteSpace(requirementsText)) score += 5;
        if (!string.IsNullOrWhiteSpace(tagsJson) && tagsJson.Contains('[')) score += 5;
        if (!string.IsNullOrWhiteSpace(sourceUrl) && Uri.TryCreate(sourceUrl, UriKind.Absolute, out _)) score += 5;
        return Math.Clamp(score, 0, 100);
    }
}
