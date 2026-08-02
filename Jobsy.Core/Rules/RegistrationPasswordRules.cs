namespace Jobsy.Core.Rules;

/// <summary>Password rules for company self-registration (chosen at submit, verified via e-mail).</summary>
public static class RegistrationPasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

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

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Wachtwoord moet minimaal één letter en één cijfer bevatten.");
        }
    }
}
