namespace Jobsy.Core.Rules;

/// <summary>Password rules for company self-registration (chosen at submit, verified via e-mail).</summary>
public static class RegistrationPasswordRules
{
    public const int MinLength = 12;
    public const int MaxLength = 128;
    // Kept local so registration remains available when an external breach service is unavailable.
    private static readonly HashSet<string> CommonOrBreachedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "password123!", "welkom123", "welkom123!",
        "qwerty123", "qwerty123!", "123456789012", "1234567890",
        "letmein123", "administrator", "admin123456", "jobsy123!"
    };

    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Wachtwoord is verplicht.");
        }

        if (password.Length < MinLength)
        {
            throw new ArgumentException($"Wachtwoord moet minimaal {MinLength} tekens zijn.");
        }

        if (password.Length > MaxLength)
        {
            throw new ArgumentException($"Wachtwoord mag maximaal {MaxLength} tekens zijn.");
        }

        if (CommonOrBreachedPasswords.Contains(password.Trim()))
        {
            throw new ArgumentException("Kies een minder vaak gebruikt wachtwoord.");
        }
    }
}
