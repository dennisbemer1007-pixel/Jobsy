namespace Jobsy.Core.Rules;

/// <summary>Reference relationship types for candidate referees (employer sees neutral role label only).</summary>
public static class ReferenceTypeCatalog
{
    public const string Manager = "manager";
    public const string Colleague = "colleague";
    public const string RegularClient = "regular-client";
    public const string Coach = "coach";
    public const string CareWorker = "care-worker";
    public const string Teacher = "teacher";

    public static IReadOnlyList<string> All { get; } =
    [
        Manager,
        Colleague,
        RegularClient,
        Coach,
        CareWorker,
        Teacher
    ];

    public static string? Normalize(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        return All.Contains(raw, StringComparer.Ordinal) ? raw : null;
    }

    public static string EmployerConfirmationLabel(string? typeCode, string language)
    {
        var code = Normalize(typeCode) ?? Manager;
        var nl = code switch
        {
            Manager => "een leidinggevende",
            Colleague => "een collega",
            RegularClient => "een vaste klant",
            Coach => "een begeleider",
            CareWorker => "een hulpverlener",
            Teacher => "een docent of stagebegeleider",
            _ => "een referent"
        };

        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
        {
            return code switch
            {
                Manager => "a manager",
                Colleague => "a colleague",
                RegularClient => "a regular client",
                Coach => "a supervisor",
                CareWorker => "a care worker",
                Teacher => "a teacher or internship supervisor",
                _ => "a reference"
            };
        }

        return nl;
    }

    public static string DisplayTitleKey(string? typeCode)
        => Normalize(typeCode) switch
        {
            Manager => "SensitiveRef.Type.Manager",
            Colleague => "SensitiveRef.Type.Colleague",
            RegularClient => "SensitiveRef.Type.RegularClient",
            Coach => "SensitiveRef.Type.Coach",
            CareWorker => "SensitiveRef.Type.CareWorker",
            Teacher => "SensitiveRef.Type.Teacher",
            _ => "SensitiveRef.Type.Manager"
        };
}
