namespace Jobsy.Core.Rules;

/// <summary>Deterministic per-session ordering for featured vacancies.</summary>
public static class HighlightShuffleRules
{
    public static uint Rank(uint seed, Guid id)
    {
        unchecked
        {
            var h = seed;
            foreach (var b in id.ToByteArray())
            {
                h ^= b;
                h *= 16777619u;
            }

            return h;
        }
    }
}
