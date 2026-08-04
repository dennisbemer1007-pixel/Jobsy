namespace Jobsy.Core.Rules;

/// <summary>
/// Catalog of optional vacancy create-form fields that admins can enable per category.
/// Values are stored on the vacancy as JSON (<c>CategoryFieldsJson</c>).
/// </summary>
public static class VacancyCategoryExtraFields
{
    public const string EducationLevel = "educationLevel";
    public const string InternshipDuration = "internshipDuration";
    public const string HoursPerWeek = "hoursPerWeek";
    public const string InternshipType = "internshipType";
    public const string OrganizationType = "organizationType";
    public const string VogRequired = "vogRequired";
    public const string Frequency = "frequency";
    public const string TargetGroup = "targetGroup";
    public const string JobCoachAvailable = "jobCoachAvailable";
    public const string WorkplaceAdjustments = "workplaceAdjustments";
    public const string PhysicalLoad = "physicalLoad";
    public const string ContractType = "contractType";
    public const string ExperienceLevel = "experienceLevel";

    public sealed record FieldDefinition(
        string Key,
        string Label,
        string InputType,
        IReadOnlyList<string>? Options = null);

    public static readonly IReadOnlyList<FieldDefinition> All =
    [
        new(EducationLevel, "Opleidingsniveau", "select",
            ["VMBO", "MBO", "HBO", "WO", "Anders"]),
        new(InternshipDuration, "Duur van de stage", "text"),
        new(HoursPerWeek, "Aantal uren per week", "text"),
        new(InternshipType, "Type stage", "select",
            ["Meeloop", "Afstudeer", "Anders"]),
        new(OrganizationType, "Organisatietype", "text"),
        new(VogRequired, "VOG vereist", "boolean"),
        new(Frequency, "Frequentie", "select",
            ["Wekelijks", "Maandelijks", "Incidenteel"]),
        new(TargetGroup, "Doelgroep", "select",
            ["Wajong", "WIA", "Participatiewet", "Anders"]),
        new(JobCoachAvailable, "Jobcoach beschikbaar", "boolean"),
        new(WorkplaceAdjustments, "Mogelijke werkplekaanpassingen", "textarea"),
        new(PhysicalLoad, "Fysieke belasting", "select",
            ["Licht", "Middel"]),
        new(ContractType, "Contracttype", "select",
            ["Oproep", "Parttime", "Fulltime", "Tijdelijk", "Vast"]),
        new(ExperienceLevel, "Ervaringsniveau", "select",
            ["Starter", "Medior", "Senior", "Geen ervaring vereist"])
    ];

    private static readonly Dictionary<string, FieldDefinition> ByKey =
        All.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);

    public static bool IsKnown(string? key)
        => !string.IsNullOrWhiteSpace(key) && ByKey.ContainsKey(key.Trim());

    public static FieldDefinition? Get(string key)
        => ByKey.TryGetValue(key, out var def) ? def : null;

    public static IReadOnlyList<string> NormalizeKeys(IEnumerable<string>? keys)
    {
        if (keys is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var raw in keys)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var key = raw.Trim();
            if (!ByKey.ContainsKey(key) || !seen.Add(key))
            {
                continue;
            }

            result.Add(ByKey[key].Key);
        }

        return result;
    }

    public static string SerializeKeys(IEnumerable<string>? keys)
        => System.Text.Json.JsonSerializer.Serialize(NormalizeKeys(keys));

    public static IReadOnlyList<string> DeserializeKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            return NormalizeKeys(parsed);
        }
        catch
        {
            return [];
        }
    }
}
