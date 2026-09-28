namespace Jobsy.Core.Enums;

/// <summary>Need-to-know scopes for temporary admin support access (prompt 06).</summary>
[Flags]
public enum SupportAccessScope
{
    None = 0,
    Contact = 1,
    Cv = 2,
    Iban = 4,
    Applications = 8,
    Feedback = 16,
    All = Contact | Cv | Iban | Applications | Feedback
}
