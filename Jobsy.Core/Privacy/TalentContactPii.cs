using Jobsy.Core.Entities;

namespace Jobsy.Core.Privacy;

/// <summary>
/// The exact candidate fields an employer receives once the candidate says yes to a talent
/// contact request. The share-preview dialog and the revealed employer DTO both go through
/// <see cref="For"/> so the promise shown to the candidate cannot drift from what is shared.
/// </summary>
public sealed record TalentContactPii(string? Name, string? Email, string? Phone)
{
    public static TalentContactPii None { get; } = new(null, null, null);

    public static TalentContactPii For(User? user)
        => user is null
            ? None
            : new TalentContactPii(
                Blank(user.FullName),
                Blank(user.Email),
                Blank(user.PhoneNumber));

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
