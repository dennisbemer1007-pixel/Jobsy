namespace Jobsy.Core.Rules;

/// <summary>
/// Stored values for <c>TalentContactRequest.CandidateDeclineReason</c> (D14). Null means the
/// candidate declined before the field existed and is read as <see cref="NotInterested"/>.
/// </summary>
public static class TalentContactDeclineReasons
{
    public const int MaxLength = 24;

    public const string NotInterested = "NotInterested";

    public const string AlreadyPlaced = "AlreadyPlaced";

    /// <summary>Normalizes a stored or incoming value; anything unknown becomes NotInterested.</summary>
    public static string Normalize(string? reason)
        => string.Equals(reason, AlreadyPlaced, StringComparison.OrdinalIgnoreCase)
            ? AlreadyPlaced
            : NotInterested;

    public static string ForResponse(bool alreadyPlaced)
        => alreadyPlaced ? AlreadyPlaced : NotInterested;
}
