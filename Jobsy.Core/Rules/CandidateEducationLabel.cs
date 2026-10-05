namespace Jobsy.Core.Rules;

/// <summary>One education string for the career gate, from the profile list plus direction.</summary>
public static class CandidateEducationLabel
{
    public static string? From(IEnumerable<string>? educations, string? direction)
    {
        var parts = (educations ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();
        if (!string.IsNullOrWhiteSpace(direction))
        {
            parts.Add(direction.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
}
