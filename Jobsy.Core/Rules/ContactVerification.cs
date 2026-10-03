using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>E-mail and phone verification stamps. A different phone number drops the phone stamp.</summary>
public static class ContactVerification
{
    public static void MarkEmailVerified(User user, DateTime utcNow)
    {
        if (user.Role != UserRole.Candidate || user.EmailVerifiedAtUtc is not null)
        {
            return;
        }

        user.EmailVerifiedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    /// <summary>
    /// Sets the phone. When the normalized number changes, the verification stamp is cleared.
    /// </summary>
    public static void ApplyPhone(User user, string? normalizedPhone)
    {
        var previous = CandidatePhoneRules.Normalize(user.PhoneNumber);
        var next = CandidatePhoneRules.Normalize(normalizedPhone);
        if (!string.Equals(previous, next, StringComparison.Ordinal))
        {
            user.PhoneVerifiedAtUtc = null;
            user.PhoneVerifiedE164 = null;
        }

        user.PhoneNumber = next;
    }
}
