namespace Jobsy.Core.Rules;

public static class InitiativeCandidateDisplay
{
    /// <summary>Stable short employer-facing candidate code (e.g. K-4821), not a raw GUID fragment.</summary>
    public static string ShortCode(Guid candidateUserId)
    {
        unchecked
        {
            var n = (Math.Abs(candidateUserId.GetHashCode()) % 9000) + 1000;
            return $"K-{n}";
        }
    }

    public static string EmployerListLabel(Guid candidateUserId)
        => $"Kandidaat {ShortCode(candidateUserId)}";
}
