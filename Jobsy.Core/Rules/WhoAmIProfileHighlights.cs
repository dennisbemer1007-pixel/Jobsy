using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Anonymised profile bits woven into the Wie ben ik? story (no company names in AI prompt).</summary>
public sealed record WhoAmIProfileHighlights(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Educations,
    IReadOnlyList<string> Certificates)
{
    public static WhoAmIProfileHighlights Empty { get; } = new([], [], []);

    public bool HasAny => Roles.Count > 0 || Educations.Count > 0 || Certificates.Count > 0;

    public static WhoAmIProfileHighlights FromPreferences(CandidatePreferencesDto? prefs)
    {
        if (prefs is null)
        {
            return Empty;
        }

        var roles = (prefs.Employers ?? [])
            .Select(e => string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        var educations = (prefs.Educations ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        var certificates = (prefs.Certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => c.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        return new WhoAmIProfileHighlights(roles, educations, certificates);
    }

    public string FingerprintSuffix()
        => string.Join('|',
            string.Join(',', Roles),
            string.Join(',', Educations),
            string.Join(',', Certificates));
}
