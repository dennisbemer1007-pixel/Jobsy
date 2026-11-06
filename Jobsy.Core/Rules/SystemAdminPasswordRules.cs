namespace Jobsy.Core.Rules;

/// <summary>Strong password rules for the break-glass system admin env secret.</summary>
public static class SystemAdminPasswordRules
{
    public const int MinLength = 16;
    public const int MaxLength = 128;

    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("SystemAdmin password is required when SystemAdmin:Enabled is true.");
        }

        if (password.Length < MinLength)
        {
            throw new ArgumentException(
                $"SystemAdmin password must be at least {MinLength} characters when SystemAdmin:Enabled is true.");
        }

        if (password.Length > MaxLength)
        {
            throw new ArgumentException(
                $"SystemAdmin password must be at most {MaxLength} characters when SystemAdmin:Enabled is true.");
        }

        RegistrationPasswordRules.Validate(password, required: true);
    }
}
